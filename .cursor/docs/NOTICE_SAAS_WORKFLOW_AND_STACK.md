# Tax Notice SaaS Workflow, Modules, and Scale Architecture

## Key Finding From Competitor Screens

The signup terms explicitly mention that data extraction from portals such as Income Tax, GST, and Report Insights is performed using web automation based on customer authorization. Customers authorize this by providing credentials.

This means the competitor is likely not using a public Income Tax notice API. Their model is closer to:

1. Customer creates account.
2. Customer adds portal credentials.
3. Customer selects sync frequency.
4. Backend automation logs in to the relevant portal.
5. System fetches notices, proceedings, orders, demands, and documents.
6. Data is normalized and stored in the SaaS database.
7. Users manage notices through dashboard workflows.

## Product Workflow

### 1. Signup and Workspace Creation

- User signs up with first name, last name, email, contact number, company name, company type, and password.
- Company type options:
  - CA
  - Corporate
  - Other
- User accepts terms and privacy policy.
- Optional Microsoft sign-in.
- Workspace/organization is created.

### 2. Team and Role Setup

- Owner invites team members.
- Roles are assigned:
  - Owner
  - Admin
  - Manager
  - Staff
  - Client viewer
- Permissions control clients, notices, documents, billing, and settings.

### 3. Client Onboarding

- User adds a client/PAN/GSTIN.
- User selects category:
  - Income Tax
  - GST
  - ITR
  - Insight Report
- User selects sync frequency:
  - Daily
  - Weekly
  - Midweek
  - Fortnightly
  - Monthly
- User provides portal username and password.
- System stores credentials in an encrypted vault.
- Sync schedule is created.

### 4. Authorized Portal Sync

- Scheduler identifies due sync jobs.
- Worker decrypts credentials inside a secure worker environment.
- Worker logs in to the portal.
- OTP/CAPTCHA is handled through assisted user flow where required.
- Worker extracts:
  - Notice list
  - Proceedings
  - Outstanding demands
  - Direct orders
  - Case status
  - Notice PDFs
  - Order/acknowledgement files
- Worker normalizes and stores data.
- Sync logs are saved.
- User is notified when sync completes or fails.

### 5. Notice Management

- User views all notices for a client.
- Notice table supports:
  - Search
  - Filters
  - Status filter
  - Grand total
  - Export
  - PDF access
- Notice fields:
  - PAN
  - Assessee name
  - Proceeding ID
  - Notice section/description
  - DIN / document reference ID
  - Financial year
  - Notice date
  - Due date
  - Status
  - Notice document

### 6. Notice Detail Workflow

- User opens individual notice.
- System shows:
  - Proceeding name
  - Proceeding ID
  - PAN
  - Assessee name
  - Financial year
  - Served date
  - Response submitted date
  - Document reference ID
  - Notice section
  - Description
  - Attachments
  - Order attachments
- User can:
  - Update status
  - Check case status
  - Set reminder
  - Upload reply documents
  - Add comments
  - Assign to team member
  - View timeline
  - Use AI assistance

### 7. AI Assistance

- AI reads notice PDF and metadata.
- AI generates:
  - Notice summary
  - Risk points
  - Required documents
  - Response checklist
  - Draft response
- User reviews and edits before use.

### 8. Billing and Sync Credits

- Each sync can consume credits.
- Plans define:
  - Users
  - Clients
  - Sync credits
  - Storage
  - AI usage
- Billing module tracks invoices, payments, and renewals.

## Main Modules

- Authentication
- Signup and onboarding
- Workspace management
- Team and roles
- Client management
- Credential vault
- Income Tax sync
- GST sync
- ITR workflow
- Insight report workflow
- Notice list
- Notice detail
- Outstanding demand
- Direct orders
- Manual notices
- Case status
- Document management
- Reply document uploads
- Assignment and task management
- Comments
- Reminders and calendar
- Status timeline
- AI notice assistant
- Notifications
- Billing and sync credits
- Audit logs
- Admin settings
- Reports and analytics

## Data Model

Core tables:

- organizations
- users
- organization_members
- roles
- clients
- client_identifiers
- portal_credentials
- sync_schedules
- sync_jobs
- sync_job_logs
- notices
- notice_documents
- notice_status_events
- notice_comments
- notice_assignments
- notice_reminders
- notice_reply_documents
- outstanding_demands
- direct_orders
- manual_notices
- case_statuses
- ai_notice_summaries
- audit_logs
- subscriptions
- sync_credit_ledger
- invoices

## Recommended Technology Stack

### Frontend

- Next.js
- React
- TypeScript
- Tailwind CSS
- TanStack Query
- React Hook Form
- Zod

### Backend

- NestJS
- TypeScript
- PostgreSQL
- Prisma or Drizzle ORM
- Redis
- BullMQ for MVP queues
- Temporal for durable production workflows

### Sync Automation

- Playwright workers for authorized portal automation
- Isolated worker containers
- Per-job browser sessions
- OTP/CAPTCHA assisted handoff
- Screenshot and step logs for debugging

### Storage and Search

- S3-compatible object storage for PDFs and uploads
- OpenSearch for notice/document search
- ClickHouse for analytics/events at high scale

### Infrastructure

- AWS or GCP
- Docker
- Kubernetes for production scale
- API Gateway
- CDN
- Aurora PostgreSQL or Cloud SQL
- ElastiCache Redis
- SQS/Kafka for event streaming
- OpenTelemetry
- Grafana/Loki/Prometheus

## Architecture For 1 Million Users

Use service boundaries from the beginning, even if deployment starts as a modular monolith.

Services:

- auth-service
- workspace-service
- client-service
- credential-service
- notice-service
- sync-service
- document-service
- reminder-service
- notification-service
- ai-service
- billing-service
- audit-service
- analytics-service

Async workers:

- income-tax-sync-worker
- gst-sync-worker
- notice-pdf-downloader
- notice-parser
- ai-summary-worker
- reminder-worker
- notification-worker
- export-worker

## Scale Principles

- Keep API requests fast and push long work to queues.
- Store notice PDFs in object storage, not database.
- Use signed URLs for document access.
- Partition large tables by organization or time.
- Use read replicas for reporting.
- Use OpenSearch for search instead of heavy database filters.
- Use ClickHouse for analytics and activity events.
- Use Temporal for reliable long-running sync workflows.
- Rate-limit portal sync jobs per tenant and per source.
- Run sync jobs in isolated containers.

## Security Requirements

- Customer authorization before portal access.
- Encrypted credential vault.
- KMS-managed keys.
- No plaintext passwords in logs.
- Decrypt credentials only inside sync worker.
- MFA for SaaS users.
- Role-based access control.
- Per-tenant data isolation.
- Audit trail for every credential use.
- Signed document URLs.
- Session timeout.
- IP/device logging.
- Credential revocation.
- Backup encryption.
- Legal terms covering authorized data extraction.

## MVP Plan

### Phase 1

- Signup/login
- Workspace creation
- Dashboard
- Client list
- Manual notice upload
- Notice list
- Notice detail
- Document upload/download
- Comments
- Status update
- Reminders

### Phase 2

- AI notice summary
- PDF extraction
- Reply document checklist
- Assignment workflow
- Billing plans

### Phase 3

- Credential vault
- Assisted Income Tax sync prototype
- Sync logs
- Sync credits
- Portal automation monitoring

### Phase 4

- GST sync
- ITR workflow
- Advanced analytics
- Mobile app
- Enterprise security controls

