// simulate-webhook.js
// Simulates a Paystack charge.success webhook for a given reference.
// Signs the payload with HMAC-SHA512 using your Paystack secret key,
// then POSTs it to your local webhook endpoint.

const crypto = require('crypto');

// ─── CONFIG ────────────────────────────────────────────────
// Paste your Paystack test secret key here (from appsettings.Development.json)
const PAYSTACK_SECRET_KEY = 'sk_test_89bf0bd07f87116af783c3c7082105c41c732c7a';

// The reference of the PENDING transaction you want to mark successful
const REFERENCE = 'SKH-1790881607257-1212';

// Amount in kobo (must match what's stored in the DB, or the webhook will reject)
const AMOUNT_KOBO = 500000; // ₦5,000 = 500,000 kobo

// Your local webhook URL
const WEBHOOK_URL = 'http://localhost:5167/api/payments/webhook';

// ─── PAYLOAD ──────────────────────────────────────────────
const payload = {
  event: 'charge.success',
  data: {
    reference: REFERENCE,
    amount: AMOUNT_KOBO,
    currency: 'NGN',
    status: 'success',
    channel: 'card',
    paid_at: new Date().toISOString(),
    customer: {
      email: 'studenta@example.com'
    },
    metadata: {
      purpose: 'COURSE_PURCHASE'
    }
  }
};

const rawBody = JSON.stringify(payload);

// ─── SIGNATURE ────────────────────────────────────────────
const signature = crypto
  .createHmac('sha512', PAYSTACK_SECRET_KEY)
  .update(rawBody)
  .digest('hex');

console.log('Payload:', rawBody);
console.log('Signature:', signature);
console.log('Posting to:', WEBHOOK_URL);

// ─── SEND ─────────────────────────────────────────────────
(async () => {
  try {
    const response = await fetch(WEBHOOK_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-paystack-signature': signature,
      },
      body: rawBody,
    });

    const text = await response.text();
    console.log('\nResponse status:', response.status);
    console.log('Response body:', text);
  } catch (err) {
    console.error('Request failed:', err.message);
  }
})();