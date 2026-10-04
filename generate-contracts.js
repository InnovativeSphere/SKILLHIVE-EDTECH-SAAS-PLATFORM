// generate-contracts.js — v2
//
// Improvements over v1:
//   1. Parses controllers to map endpoints → service methods (fixes auth duplication)
//   2. Follows service methods that delegate to helper methods
//   3. Extracts `select new { }` LINQ projections
//   4. Cleaner type inference, no raw C# in output
//   5. Documents the standard response envelope and auth model
//
// PREREQUISITE: Server running on http://localhost:5167
// USAGE: node generate-contracts.js

const fs = require('fs');
const path = require('path');

const BASE_URL = process.env.SKILLHIVE_URL || 'http://localhost:5167';
const ROOT = process.cwd();

const read = (p) => fs.readFileSync(p, 'utf8');
const write = (p, c) => fs.writeFileSync(p, c);

// ─────────────────────────────────────────────────────────────
// FILE WALKER
// ─────────────────────────────────────────────────────────────

function walk(dir, filter, out = []) {
  if (!fs.existsSync(dir)) return out;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (['bin', 'obj', 'node_modules', '.git', '.vs', '.vscode'].includes(entry.name)) continue;
      walk(full, filter, out);
    } else if (filter(entry.name)) out.push(full);
  }
  return out;
}

// ─────────────────────────────────────────────────────────────
// BRACE MATCHER (string/comment aware)
// ─────────────────────────────────────────────────────────────

function extractBlock(content, startBraceIndex) {
  let depth = 0, inString = false, inChar = false, lineComment = false, blockComment = false;
  for (let i = startBraceIndex; i < content.length; i++) {
    const c = content[i], next = content[i + 1];
    if (lineComment) { if (c === '\n') lineComment = false; continue; }
    if (blockComment) { if (c === '*' && next === '/') { blockComment = false; i++; } continue; }
    if (inString) { if (c === '\\') i++; else if (c === '"') inString = false; continue; }
    if (inChar) { if (c === '\\') i++; else if (c === "'") inChar = false; continue; }
    if (c === '/' && next === '/') { lineComment = true; i++; continue; }
    if (c === '/' && next === '*') { blockComment = true; i++; continue; }
    if (c === '"') { inString = true; continue; }
    if (c === "'") { inChar = true; continue; }
    if (c === '{') depth++;
    else if (c === '}') { depth--; if (depth === 0) return content.substring(startBraceIndex + 1, i); }
  }
  return null;
}

// ─────────────────────────────────────────────────────────────
// ENUM PARSER
// ─────────────────────────────────────────────────────────────

function parseEnums(content, file) {
  const out = [];
  const regex = /public\s+enum\s+(\w+)\s*\{/g;
  let m;
  while ((m = regex.exec(content)) !== null) {
    const name = m[1];
    const braceIndex = content.indexOf('{', m.index);
    const body = extractBlock(content, braceIndex);
    if (!body) continue;
    const values = body
      .split(/[,\r\n]/)
      .map(s => s.replace(/\/\/.*$/, '').replace(/\/\*[\s\S]*?\*\//g, '').trim())
      .filter(s => s && /^\w+$/.test(s));
    out.push({ name, values });
  }
  return out;
}

// ─────────────────────────────────────────────────────────────
// DTO PARSER
// ─────────────────────────────────────────────────────────────

function parseDto(content, file) {
  const classMatch = content.match(/public\s+class\s+(\w+Dto)\b/);
  if (!classMatch) return null;
  const className = classMatch[1];

  const braceIndex = content.indexOf('{', classMatch.index);
  const body = extractBlock(content, braceIndex);
  if (!body) return null;

  const props = [];
  const propRegex = /((?:\[[^\]]*\]\s*)+)?\s*public\s+([\w\?<>\.\[\]]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}/g;
  let m;
  while ((m = propRegex.exec(body)) !== null) {
    const attrs = m[1] || '';
    props.push({
      name: m[3],
      type: normalizeType(m[2]),
      required: /\[Required\]/.test(attrs),
      attributes: extractAttributes(attrs),
    });
  }

  return { className, properties: props, sourceFile: path.relative(ROOT, file).replace(/\\/g, '/') };
}

function extractAttributes(attrs) {
  const out = [];
  const regex = /\[(\w+)(?:\(([^)]*)\))?\]/g;
  let m;
  while ((m = regex.exec(attrs)) !== null) {
    out.push({ name: m[1], args: m[2] ? m[2].split(',').map(s => s.trim()) : [] });
  }
  return out;
}

function normalizeType(t) {
  const raw = t.trim();
  const nullable = raw.endsWith('?');
  const base = nullable ? raw.slice(0, -1) : raw;
  if (base.startsWith('List<') || base.startsWith('IEnumerable<') || base.startsWith('ICollection<')) {
    const inner = base.match(/<(.+)>/)[1];
    return { type: 'array', items: normalizeType(inner), nullable };
  }
  if (base === 'string') return { type: 'string', nullable };
  if (base === 'int' || base === 'long' || base === 'short') return { type: 'integer', nullable };
  if (base === 'decimal' || base === 'double' || base === 'float') return { type: 'number', nullable };
  if (base === 'bool') return { type: 'boolean', nullable };
  if (base === 'DateTime') return { type: 'datetime', nullable };
  return { type: base, nullable };
}

// ─────────────────────────────────────────────────────────────
// CONTROLLER PARSER — build route → service method map
// ─────────────────────────────────────────────────────────────

function parseController(content, file) {
  const endpoints = [];

  // Controller-level route
  const controllerRouteMatch = content.match(/\[Route\("([^"]+)"\)\]/);
  const controllerRoute = controllerRouteMatch ? controllerRouteMatch[1] : '';

  // Find all [Http*] attributes
  const httpRegex = /\[Http(Get|Post|Put|Patch|Delete)(?:\("([^"]*)"\))?\]/g;
  let m;
  while ((m = httpRegex.exec(content)) !== null) {
    const verb = m[1].toUpperCase();
    const routeTemplate = m[2] !== undefined ? m[2] : '';

    // Build the full route
    let fullRoute;
    if (routeTemplate.startsWith('/')) {
      fullRoute = routeTemplate;
    } else if (routeTemplate) {
      fullRoute = '/' + controllerRoute.replace(/^\/|\/$/g, '') + '/' + routeTemplate.replace(/^\/|\/$/g, '');
    } else {
      fullRoute = '/' + controllerRoute.replace(/^\/|\/$/g, '');
    }
    fullRoute = fullRoute.replace(/\/+/g, '/');

    // Look BACKWARD from [Http*] for [Authorize] / [AllowAnonymous]
    const beforeAttr = content.substring(Math.max(0, m.index - 500), m.index);
    const authMatch = beforeAttr.match(/\[Authorize\(Roles\s*=\s*"([^"]+)"\)\]\s*(?:\r?\n\s*)?$/m);
    const anonymousMatch = /\[AllowAnonymous\]\s*(?:\r?\n\s*)?$/m.test(beforeAttr);

    // Look FORWARD for the method signature and body
    const afterAttr = content.substring(m.index + m[0].length);
    const methodMatch = afterAttr.match(/public\s+async\s+Task<[\w<>\[\]]+>\s+(\w+)\s*\(([^)]*)\)/);
    if (!methodMatch) continue;
    const methodName = methodMatch[1];

    // Find the method body
    const bodyStart = afterAttr.indexOf('{', methodMatch.index);
    const body = extractBlock(afterAttr, bodyStart);
    if (!body) continue;

    // Find the service call: await _something.ServiceMethod(...)
    const serviceCallMatch = body.match(/await\s+_(\w+)\s*\.\s*(\w+)\s*\(/);
    const serviceField = serviceCallMatch ? serviceCallMatch[1] : null;
    const serviceMethod = serviceCallMatch ? serviceCallMatch[2] : null;

    endpoints.push({
      verb,
      route: fullRoute,
      controllerMethod: methodName,
      serviceField,
      serviceMethod,
      roles: authMatch ? authMatch[1].split(',').map(r => r.trim()) : null,
      allowAnonymous: anonymousMatch,
      sourceFile: path.relative(ROOT, file).replace(/\\/g, '/'),
    });
  }
  return endpoints;
}

// Convert field name like "subscriptionService" to class name "SubscriptionService"
function fieldToServiceClass(field) {
  if (!field) return null;
  return field.charAt(0).toUpperCase() + field.slice(1);
}

// ─────────────────────────────────────────────────────────────
// SERVICE PARSER — extract response shapes per method
// ─────────────────────────────────────────────────────────────

function parseService(content, file) {
  const publicMethods = {};
  const helpers = {};

  // Find all method signatures (public and private)
  // Pattern: [modifiers] Task<ReturnType> MethodName(...)
  const methodRegex = /(public|private|protected|internal)\s+(static\s+)?(async\s+)?Task<([\w<>\[\]\?]+)>\s+(\w+)\s*\(/g;
  let m;
  while ((m = methodRegex.exec(content)) !== null) {
    const visibility = m[1];
    const isStatic = !!m[2];
    const methodName = m[5];

    const bodyStart = content.indexOf('{', m.index);
    if (bodyStart === -1) continue;
    const body = extractBlock(content, bodyStart);
    if (!body) continue;

    const shapes = [];
    const helperCalls = [];

    // Find all return statements in this method body
    const returnRegex = /return\s+(?:await\s+)?([^;]+);/g;
    let rm;
    while ((rm = returnRegex.exec(body)) !== null) {
      const expr = rm[1].trim();
      if (expr.startsWith('new')) {
        const braceIdx = expr.indexOf('{');
        if (braceIdx !== -1) {
          const objBody = extractBlock(expr, braceIdx);
          if (objBody) {
            const props = extractTopLevelProps(objBody);
            if (props.length > 0) shapes.push({ properties: props });
          }
        }
      } else if (/^(\w+)\s*\(/.test(expr) || /^\w+\s*\.\s*\w+\s*\(/.test(expr)) {
        // return HelperMethod(...) or return this.Helper(...)
        const helperMatch = expr.match(/^(?:this\.)?(\w+)\s*\(/);
        if (helperMatch) helperCalls.push(helperMatch[1]);
      }
    }

    // Also grab LINQ projections: .Select(x => new { ... })
    const selectRegex = /\.Select\s*\(\s*\w+\s*=>\s*new\s*\{/g;
    let sm;
    while ((sm = selectRegex.exec(body)) !== null) {
      const braceIdx = body.indexOf('{', sm.index);
      const objBody = extractBlock(body, braceIdx);
      if (objBody) {
        const props = extractTopLevelProps(objBody);
        if (props.length > 0) shapes.push({ properties: props, source: 'select-projection' });
      }
    }

    const entry = { shapes, helperCalls, isStatic, visibility };

    if (visibility === 'public' && !isStatic) {
      publicMethods[methodName] = entry;
    } else {
      helpers[methodName] = entry;
    }
  }

  return { publicMethods, helpers };
}

// ─────────────────────────────────────────────────────────────
// ANONYMOUS OBJECT PROPERTY EXTRACTION
// ─────────────────────────────────────────────────────────────

function extractTopLevelProps(block) {
  const props = [];
  let depth = 0, inString = false, segment = '';

  for (let i = 0; i < block.length; i++) {
    const c = block[i];
    if (inString) {
      segment += c;
      if (c === '\\') { segment += block[++i] || ''; continue; }
      if (c === '"') inString = false;
      continue;
    }
    if (c === '"') { inString = true; segment += c; continue; }
    if (c === '{' || c === '(' || c === '[') depth++;
    else if (c === '}' || c === ')' || c === ']') depth--;
    if (c === ',' && depth === 0) { pushProp(segment, props); segment = ''; }
    else segment += c;
  }
  pushProp(segment, props);
  return props;
}

function pushProp(segment, out) {
  const s = segment.trim();
  if (!s) return;
  const match = s.match(/^(\w+)\s*=\s*([\s\S]+)$/);
  if (!match) return;
  out.push({
    name: match[1],
    type: inferType(match[2]),
    expression: match[2].trim().replace(/\s+/g, ' ').slice(0, 60),
  });
}

function inferType(expr) {
  const e = expr.trim().replace(/\s+/g, ' ');
  if (e === 'null') return 'null';
  if (/^new\s*\{/.test(e)) return 'object';
  if (/^new\s*\[\]/.test(e) || /^new\s+List</.test(e)) return 'array';
  if (/^".*"$/.test(e)) return 'string';
  if (/^-?\d+(\.\d+)?m?f?d?$/.test(e)) return 'number';
  if (/^(true|false)$/.test(e)) return 'boolean';
  if (/DateTime\.(UtcNow|Now)/.test(e)) return 'datetime';
  if (/\.ToString\(\)$/.test(e)) return 'enum-as-string';
  if (/\.ToString\(\)$/.test(e)) return 'enum-as-string';
  if (/^\w+$/.test(e)) return 'variable';
  if (/\.Count$|\.Length$/.test(e)) return 'integer';
  if (/^\w+\.\w+(\.\w+)*$/.test(e)) return 'expression';
  return 'unknown';
}

// ─────────────────────────────────────────────────────────────
// SHAPE RESOLUTION — follow helper chains
// ─────────────────────────────────────────────────────────────

function resolveShapes(serviceMethod, service, visited = new Set()) {
  if (!serviceMethod || !service.publicMethods[serviceMethod]) return [];

  const entry = service.publicMethods[serviceMethod];
  let shapes = [...(entry.shapes || [])];

  for (const helperName of entry.helperCalls || []) {
    if (visited.has(helperName)) continue;
    visited.add(helperName);
    const helper = service.helpers[helperName];
    if (helper && helper.shapes) {
      shapes = shapes.concat(helper.shapes);
    }
    // Recurse in case helper returns another helper
    if (helper && helper.helperCalls) {
      for (const nestedHelper of helper.helperCalls) {
        if (visited.has(nestedHelper)) continue;
        visited.add(nestedHelper);
        const nested = service.helpers[nestedHelper];
        if (nested && nested.shapes) shapes = shapes.concat(nested.shapes);
      }
    }
  }

  return shapes;
}

// ─────────────────────────────────────────────────────────────
// SWAGGER HELPERS
// ─────────────────────────────────────────────────────────────

async function fetchSwagger() {
  const url = `${BASE_URL}/swagger/v1/swagger.json`;
  console.log(`🔍 Fetching Swagger from ${url} ...`);
  try {
    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
  } catch (e) {
    console.error('\n❌ Could not fetch Swagger.');
    console.error('   Make sure the server is running in Development mode: dotnet run');
    console.error(`   Error: ${e.message}\n`);
    process.exit(1);
  }
}

function normalizeRoute(route) {
  // Strip leading slash, trailing slash; lowercase for matching
  return route.replace(/^\//, '').replace(/\/$/, '').toLowerCase();
}

function inferRequestBody(requestBody, dtos) {
  const content = requestBody?.content?.['application/json'];
  if (!content || !content.schema) return null;
  const schema = content.schema;
  if (schema.$ref) {
    const refName = schema.$ref.split('/').pop();
    const dto = dtos[refName];
    if (dto) return { type: refName, properties: dto.properties };
    return { type: refName, properties: null, warning: 'DTO source not parsed' };
  }
  return { type: 'inline', schema };
}

// ─────────────────────────────────────────────────────────────
// MAIN
// ─────────────────────────────────────────────────────────────

async function main() {
  const swagger = await fetchSwagger();
  console.log(`✅ Swagger returned ${Object.keys(swagger.paths || {}).length} paths.`);

  console.log('\n📂 Scanning source tree...');
  const dtoFiles = walk(path.join(ROOT, 'Features'), f => /Dto\.cs$/.test(f));
  const serviceFiles = walk(path.join(ROOT, 'Features'), f => /Service\.cs$/.test(f));
  const controllerFiles = walk(path.join(ROOT, 'Features'), f => /Controller\.cs$/.test(f));
  const enumFiles = walk(path.join(ROOT, 'Enums'), f => f.endsWith('.cs'));
  console.log(`   DTOs: ${dtoFiles.length} | Services: ${serviceFiles.length} | Controllers: ${controllerFiles.length} | Enums: ${enumFiles.length}`);

  // ─── Parse DTOs
  const dtos = {};
  for (const file of dtoFiles) {
    try {
      const dto = parseDto(read(file), file);
      if (dto) dtos[dto.className] = dto;
    } catch (e) { console.warn(`   ⚠️  DTO ${path.basename(file)}: ${e.message}`); }
  }
  console.log(`✅ Parsed ${Object.keys(dtos).length} DTOs.`);

  // ─── Parse Enums
  const enums = {};
  for (const file of enumFiles) {
    try {
      const found = parseEnums(read(file), file);
      for (const e of found) enums[e.name] = { ...e, sourceFile: path.relative(ROOT, file).replace(/\\/g, '/') };
    } catch (e) { console.warn(`   ⚠️  Enum ${path.basename(file)}: ${e.message}`); }
  }
  console.log(`✅ Parsed ${Object.keys(enums).length} enums.`);

  // ─── Parse Controllers → build route → service method map
  const controllerEndpoints = [];
  for (const file of controllerFiles) {
    try {
      const eps = parseController(read(file), file);
      controllerEndpoints.push(...eps);
    } catch (e) { console.warn(`   ⚠️  Controller ${path.basename(file)}: ${e.message}`); }
  }
  console.log(`✅ Parsed ${controllerEndpoints.length} controller actions.`);

  // Build map: "GET /api/courses" → { serviceClass, serviceMethod, roles }
  const routeMap = {};
  for (const ep of controllerEndpoints) {
    const key = `${ep.verb} ${normalizeRoute(ep.route)}`;
    routeMap[key] = {
      serviceClass: fieldToServiceClass(ep.serviceField),
      serviceMethod: ep.serviceMethod,
      roles: ep.roles,
      allowAnonymous: ep.allowAnonymous,
    };
  }

  // ─── Parse Services → method shapes
  const services = {};
  for (const file of serviceFiles) {
    try {
      const name = path.basename(file, '.cs');
      const parsed = parseService(read(file), file);
      services[name] = parsed;
    } catch (e) { console.warn(`   ⚠️  Service ${path.basename(file)}: ${e.message}`); }
  }
  console.log(`✅ Parsed ${Object.keys(services).length} services.`);

  // ─── Build endpoint entries
  const endpoints = [];
  for (const [route, methods] of Object.entries(swagger.paths || {})) {
    for (const [method, op] of Object.entries(methods)) {
      const verb = method.toUpperCase();
      const mapKey = `${verb} ${normalizeRoute(route)}`;
      const mapping = routeMap[mapKey];

      let responseShapes = [];
      let shapeSource = 'none';

      if (mapping && mapping.serviceClass && mapping.serviceMethod) {
        const service = services[mapping.serviceClass];
        if (service) {
          responseShapes = resolveShapes(mapping.serviceMethod, service);
          shapeSource = responseShapes.length > 0 ? 'extracted' : 'method-not-found';
        } else {
          shapeSource = `service ${mapping.serviceClass} not parsed`;
        }
      } else {
        shapeSource = 'no controller mapping';
      }

      endpoints.push({
        route,
        method: verb,
        summary: op.summary || '',
        tags: op.tags || [],
        auth: mapping?.allowAnonymous ? 'public'
              : mapping?.roles ? mapping.roles.join(', ')
              : ((op.security && op.security.length > 0) ? 'authenticated' : 'unknown'),
        parameters: (op.parameters || []).map(p => ({
          name: p.name, in: p.in, required: !!p.required,
          type: p.schema?.type || p.type || 'unknown',
        })),
        requestBody: op.requestBody ? inferRequestBody(op.requestBody, dtos) : null,
        responseShapes,
        shapeSource,
        hasShape: responseShapes.length > 0,
      });
    }
  }

  endpoints.sort((a, b) => a.route.localeCompare(b.route) || a.method.localeCompare(b.method));

  const withShapes = endpoints.filter(e => e.hasShape).length;

  const contracts = {
    _meta: {
      generatedAt: new Date().toISOString(),
      server: BASE_URL,
      note: 'Response shapes are extracted from service method return statements and their helper chains. Verify flagged entries manually.',
      stats: {
        paths: endpoints.length,
        endpointsWithShapes: withShapes,
        endpointsWithoutShapes: endpoints.length - withShapes,
        dtos: Object.keys(dtos).length,
        enums: Object.keys(enums).length,
      },
      // ─── ENVELOPE ───
      envelope: {
        description: 'Every API response uses this envelope. Success or failure is determined by the "success" boolean.',
        successExample: {
          success: true,
          statusCode: 200,
          message: 'Success',
          data: '<endpoint-specific payload>',
        },
        errorExample: {
          success: false,
          statusCode: 400,
          message: 'Human-readable error description',
          data: null,
        },
        notes: [
          'HTTP status code matches statusCode in body',
          'Validation errors return 400 with a descriptive message',
          'Unauthorized → 401, Forbidden → 403, Not Found → 404, Server Error → 500',
          'data is null for errors',
        ],
      },
      // ─── AUTH MODEL ───
      auth: {
        scheme: 'JWT Bearer (with httpOnly cookie bridge)',
        tokenLifetime: '7 days',
        cookieName: 'AuthToken',
        refreshEndpoint: null,
        notes: [
          'Login returns { token, user }. The token is also set as an httpOnly cookie (7-day expiry).',
          'CookieAuthMiddleware copies the AuthToken cookie into the Authorization header automatically — so cookie-based browser clients work without sending the header explicitly.',
          'There is NO token refresh endpoint. When a token expires, the user must log in again.',
          'Roles: SUPER_ADMIN, ACADEMY_OWNER, INSTRUCTOR, MODERATOR, STUDENT.',
        ],
      },
    },
    enums,
    dtos,
    endpoints,
  };

  // ─── Write outputs
  console.log('\n💾 Writing output files...');
  write(path.join(ROOT, 'skillhive-openapi.json'), JSON.stringify(swagger, null, 2));
  console.log('   ✅ skillhive-openapi.json');
  write(path.join(ROOT, 'skillhive-contracts.json'), JSON.stringify(contracts, null, 2));
  console.log('   ✅ skillhive-contracts.json');
  write(path.join(ROOT, 'skillhive-contracts.md'), renderMarkdown(contracts));
  console.log('   ✅ skillhive-contracts.md');

  console.log('\n🎉 Done.\n');
  console.log(`   Endpoints:        ${endpoints.length}`);
  console.log(`   With shapes:      ${withShapes}`);
  console.log(`   Without shapes:   ${endpoints.length - withShapes} (verify manually)`);
  console.log(`   DTOs:             ${Object.keys(dtos).length}`);
  console.log(`   Enums:            ${Object.keys(enums).length}\n`);

  // List the ones without shapes for easy follow-up
  const missing = endpoints.filter(e => !e.hasShape);
  if (missing.length > 0) {
    console.log('   Endpoints without extracted shapes:');
    for (const e of missing.slice(0, 30)) {
      console.log(`     ${e.method.padEnd(6)} ${e.route}  (${e.shapeSource})`);
    }
    if (missing.length > 30) console.log(`     ... and ${missing.length - 30} more\n`);
    else console.log('');
  }
}

// ─────────────────────────────────────────────────────────────
// MARKDOWN RENDERER
// ─────────────────────────────────────────────────────────────

function renderMarkdown(contracts) {
  const lines = [];
  const m = contracts._meta;

  lines.push('# SkillHive — API Contracts');
  lines.push('');
  lines.push(`> Auto-generated on ${m.generatedAt}  `);
  lines.push(`> Server: ${m.server}`);
  lines.push('');
  lines.push(`**${m.stats.paths} endpoints** · **${m.stats.dtos} DTOs** · **${m.stats.enums} enums** · **${m.stats.endpointsWithShapes}/${m.stats.paths} endpoints with response shapes**`);
  lines.push('');

  // ─── Envelope
  lines.push('## Response Envelope');
  lines.push('');
  lines.push(m.envelope.description);
  lines.push('');
  lines.push('**Success:**');
  lines.push('```json');
  lines.push(JSON.stringify(m.envelope.successExample, null, 2));
  lines.push('```');
  lines.push('');
  lines.push('**Error:**');
  lines.push('```json');
  lines.push(JSON.stringify(m.envelope.errorExample, null, 2));
  lines.push('```');
  lines.push('');
  for (const note of m.envelope.notes) lines.push(`- ${note}`);
  lines.push('');

  // ─── Auth
  lines.push('## Authentication');
  lines.push('');
  lines.push(`- **Scheme:** ${m.auth.scheme}`);
  lines.push(`- **Token lifetime:** ${m.auth.tokenLifetime}`);
  lines.push(`- **Cookie name:** ${m.auth.cookieName}`);
  lines.push(`- **Refresh endpoint:** ${m.auth.refreshEndpoint || 'None'}`);
  lines.push('');
  for (const note of m.auth.notes) lines.push(`- ${note}`);
  lines.push('');

  // ─── Enums
  lines.push('---');
  lines.push('');
  lines.push('## Enums');
  lines.push('');
  const enumNames = Object.keys(contracts.enums).sort();
  for (const name of enumNames) {
    lines.push(`### \`${name}\``);
    lines.push('');
    lines.push(contracts.enums[name].values.map(v => `- \`${v}\``).join('\n'));
    lines.push('');
  }

  // ─── Endpoints
  lines.push('---');
  lines.push('');
  lines.push('## Endpoints');
  lines.push('');

  const byTag = {};
  for (const ep of contracts.endpoints) {
    const tag = ep.tags[0] || 'Other';
    if (!byTag[tag]) byTag[tag] = [];
    byTag[tag].push(ep);
  }

  for (const tag of Object.keys(byTag).sort()) {
    lines.push(`### ${tag}`);
    lines.push('');
    for (const ep of byTag[tag]) {
      lines.push(`#### \`${ep.method} ${ep.route}\``);
      lines.push('');
      if (ep.summary) { lines.push(`*${ep.summary}*`); lines.push(''); }
      lines.push(`**Auth:** ${ep.auth}`);

      if (ep.parameters.length > 0) {
        lines.push('');
        lines.push('**Parameters:**');
        for (const p of ep.parameters) {
          lines.push(`- \`${p.name}\` — ${p.in}, ${p.type}${p.required ? ' **(required)**' : ''}`);
        }
      }

      if (ep.requestBody) {
        lines.push('');
        lines.push(`**Request Body:** \`${ep.requestBody.type}\``);
        if (ep.requestBody.properties && ep.requestBody.properties.length > 0) {
          lines.push('');
          lines.push('```json');
          const ex = {};
          for (const prop of ep.requestBody.properties) ex[prop.name] = exampleValueForProp(prop);
          lines.push(JSON.stringify(ex, null, 2));
          lines.push('```');
        }
      }

      if (ep.hasShape) {
        lines.push('');
        lines.push('**Response:**');
        const merged = new Map();
        for (const shape of ep.responseShapes) {
          for (const prop of shape.properties || []) {
            if (!merged.has(prop.name)) merged.set(prop.name, prop);
          }
        }
        if (merged.size > 0) {
          lines.push('');
          lines.push('```json');
          const ex = {};
          for (const [name, prop] of merged) {
            ex[name] = exampleValueForShapeProp(prop);
          }
          lines.push(JSON.stringify(ex, null, 2));
          lines.push('```');
        }
      } else {
        lines.push('');
        lines.push(`⚠️ *Response shape could not be extracted (${ep.shapeSource}). Check the controller + service manually.*`);
      }

      lines.push('');
    }
  }

  return lines.join('\n');
}

function exampleValueForProp(prop) {
  const t = typeof prop.type === 'object' ? prop.type.type : prop.type;
  const n = typeof prop.type === 'object' ? prop.type.nullable : false;
  if (t === 'string') return n ? null : 'string';
  if (t === 'integer') return 0;
  if (t === 'number') return 0;
  if (t === 'boolean') return false;
  if (t === 'datetime') return '2026-01-01T00:00:00Z';
  if (t === 'array') return [];
  return `<${t}>`;
}

function exampleValueForShapeProp(prop) {
  const t = prop.type;
  if (t === 'null') return null;
  if (t === 'object') return {};
  if (t === 'array') return [];
  if (t === 'string') return 'string';
  if (t === 'enum-as-string') return 'ENUM_VALUE';
  if (t === 'number' || t === 'integer') return 0;
  if (t === 'boolean') return false;
  if (t === 'datetime') return '2026-01-01T00:00:00Z';
  if (t === 'variable' || t === 'expression' || t === 'unknown') return `<${t}>`;
  return `<${t}>`;
}

main().catch(err => {
  console.error('\n❌ Fatal:', err);
  process.exit(1);
});