# Encrypted API Envelope Design

## Goal

Design API payload encryption similar in spirit to the observed Notice CPC pattern, while using our own clean, documented, auditable implementation.

The purpose is to protect sensitive tax, credential, notice, and document metadata in transit in addition to TLS.

## Important Note

TLS is still mandatory. Application-level encryption is an extra defense layer, not a replacement for HTTPS.

## Observed Pattern

The observed response has fields like:

```json
{
  "v": "1",
  "p2": "...",
  "p3": "...",
  "p4": "...",
  "t": 1783781795130
}
```

Likely meaning:

- `v`: envelope version
- `p2`: AES-GCM IV / nonce
- `p3`: encrypted ciphertext
- `p4`: AES-GCM authentication tag
- `t`: timestamp

From the frontend bundle, requests appear to include additional fields:

- `p1`: RSA-OAEP encrypted AES key
- `p2`: IV
- `p3`: ciphertext
- `p4`: auth tag
- `k`: key id
- `n`: nonce
- `t`: timestamp

## Recommended Our Design

Use clear field names internally, even if we expose short field names externally.

### Request Envelope

```json
{
  "version": "1",
  "keyId": "rsa-2026-01",
  "encryptedKey": "base64-rsa-oaep-aes-key",
  "iv": "base64-12-byte-iv",
  "ciphertext": "base64-aes-gcm-ciphertext",
  "tag": "base64-aes-gcm-tag",
  "nonce": "base64-random-request-nonce",
  "timestamp": 1783781795130
}
```

### Compact Wire Format

If we want a compact wire format:

```json
{
  "v": "1",
  "k": "rsa-2026-01",
  "p1": "base64-rsa-oaep-aes-key",
  "p2": "base64-12-byte-iv",
  "p3": "base64-aes-gcm-ciphertext",
  "p4": "base64-aes-gcm-tag",
  "n": "base64-random-request-nonce",
  "t": 1783781795130
}
```

## Encryption Algorithm

Use hybrid encryption:

1. Client generates a random AES-256 key.
2. Client generates a 96-bit IV for AES-GCM.
3. Client encrypts JSON payload with AES-256-GCM.
4. Client encrypts AES key with server public key using RSA-OAEP-SHA256.
5. Client sends envelope to API.
6. API decrypts AES key using private key stored in Azure Key Vault.
7. API decrypts payload.
8. API processes request.
9. API encrypts response with the same AES key or a new response key.

## Azure Implementation

Recommended services:

- Azure Key Vault for RSA private keys
- Managed Identity for API access to Key Vault
- ASP.NET Core middleware for decrypt/encrypt
- Azure API Management for perimeter security
- Application Insights for logs, without sensitive payloads

## Request Flow

```text
Angular app
  -> generates AES key and IV
  -> encrypts request body
  -> encrypts AES key with public key
  -> sends envelope

ASP.NET Core API
  -> validates auth token
  -> validates envelope timestamp and nonce
  -> decrypts AES key via Azure Key Vault
  -> decrypts payload
  -> executes controller
  -> encrypts response
  -> returns response envelope
```

## Replay Protection

Every envelope must include:

- timestamp
- nonce
- request id
- auth user id

Server rules:

- Reject timestamps older than 2-5 minutes.
- Store nonce/request id in Redis temporarily.
- Reject duplicate nonce/request id.
- Bind nonce to user/session.

## Auth Headers

Use standard auth:

```http
Authorization: Bearer <jwt>
X-Company-Id: <tenant-id>
X-Request-Id: <uuid>
Content-Type: application/json
```

Avoid custom token names where possible. Use standard `Authorization` header.

## API Endpoint Example

```http
POST /api/v1/dynamic/query
Authorization: Bearer <jwt>
X-Company-Id: 4535_3949
X-Request-Id: 018f8e9a-...
Content-Type: application/json
```

Encrypted request:

```json
{
  "v": "1",
  "k": "rsa-2026-01",
  "p1": "...",
  "p2": "...",
  "p3": "...",
  "p4": "...",
  "n": "...",
  "t": 1783781795130
}
```

Decrypted payload:

```json
{
  "operation": "notice.list",
  "tenantId": "4535_3949",
  "filters": {
    "pan": "AAGCM4492D",
    "status": "Open"
  },
  "pagination": {
    "page": 1,
    "pageSize": 50
  }
}
```

Encrypted response:

```json
{
  "v": "1",
  "p2": "...",
  "p3": "...",
  "p4": "...",
  "t": 1783781795130
}
```

Decrypted response:

```json
{
  "data": [],
  "total": 21,
  "page": 1,
  "pageSize": 50
}
```

## ASP.NET Core Components

- `EncryptionEnvelopeMiddleware`
- `IEnvelopeCryptoService`
- `IKeyVaultCryptoService`
- `INonceReplayStore`
- `EnvelopeOptions`
- `EncryptedJsonInputFormatter` if we want MVC-level integration

## Angular Components

- `ApiEncryptionInterceptor`
- `EnvelopeCryptoService`
- `PublicKeyService`
- `NonceService`
- `ApiClient`

## Key Rotation

- Maintain `keyId`.
- Publish active public key to authenticated clients.
- Keep old private keys in Key Vault for a grace period.
- Rotate keys every 90-180 days or after incident.
- Store key version in each envelope.

## Logging Rules

Never log:

- plaintext payloads
- portal credentials
- JWTs
- auth tokens
- AES keys
- encryptedKey values
- full envelopes in production

Safe logs:

- request id
- user id
- tenant id
- operation
- status code
- latency
- sync job id
- envelope version
- key id

## When To Encrypt

Encrypt by default for:

- credentials
- notices
- tax documents
- PAN/GSTIN data
- client records
- sync job payloads
- AI prompt payloads

Optional plain JSON can be allowed for:

- public health check
- app config
- login bootstrap/public key fetch

## MVP Recommendation

Start with:

- HTTPS everywhere
- JWT auth
- Azure Key Vault
- AES-GCM request/response envelope for sensitive APIs
- Redis nonce replay protection
- strict audit logs

Then add:

- key rotation automation
- API Management policies
- private endpoints
- WAF
- mTLS for internal services

