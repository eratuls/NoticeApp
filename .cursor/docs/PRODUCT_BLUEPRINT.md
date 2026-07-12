# Tax Notice SaaS Blueprint

## Product Goal

Build a SaaS platform for CA firms, tax consultants, and finance teams to manage statutory compliance workflows across Income Tax, GST, ITR, and related client services.

The product should provide a centralized workspace for syncing notices, tracking deadlines, assigning work, uploading responses, maintaining audit trails, and generating insights.

## Core Users

- Firm owner / admin
- Tax manager
- Team member / preparer
- Client / assessee contact

## MVP Modules

### 1. Authentication and Workspace

- Email/password login
- OTP or MFA-ready login flow
- Organization/workspace setup
- User roles: owner, admin, manager, staff, client viewer
- Session timeout and activity tracking

### 2. Dashboard

- Total clients
- Total team members
- Total notices
- New, ongoing, closed, and overdue notice counts
- Module quick links
- Recent activity
- Upcoming deadlines

### 3. Client Management

- Client profile
- PAN/TAN/GSTIN identifiers
- Contact details
- Assigned team members
- Compliance category tags
- Client-wise notice summary

### 4. Income Tax Notices

- Notice list by client, PAN/TAN, status, due date, and section
- Notice detail page
- Fields:
  - Proceeding ID
  - Section
  - Financial year
  - Assessee name
  - PAN
  - Served date
  - Response due date
  - Response submitted date
  - Document reference ID
  - Status
  - Description
- Attachments:
  - Notice document
  - Acknowledgement
  - Reply documents
  - Order documents
- Status timeline
- Comments and internal remarks
- Reminder setup
- Case status tracking

### 5. GST Notices

- GST notice list
- GSTIN-wise client view
- Notice/order details
- Reply tracking
- Refund and return-compliance workflows

### 6. ITR Workflow

- Filing status by client and year
- Return due dates
- Document checklist
- Assignment and review status

### 7. Team and Task Management

- Team directory
- Assign notices/tasks to team members
- Due-date tracking
- Comment threads
- Activity log

### 8. Calendar and Reminders

- Calendar views: month, week, day
- Notice deadlines
- Manual reminders
- Email/in-app notification hooks

### 9. Documents

- Secure file upload
- Document categorization
- Download/view permissions
- Version history for reply documents

### 10. Billing and Plans

- Subscription plans
- Sync credits
- Payment history
- Upgrade/downgrade flow

## Later Modules

- AIS / 26AS insights
- ITAT case tracking
- TDS workflows
- Form 15CA/CB workflows
- AI notice summarization
- AI draft reply assistant
- Client portal
- Mobile app
- Bulk sync jobs
- API integrations with government portals where legally and technically permitted

## Recommended Tech Stack

### Frontend

- Next.js or React
- TypeScript
- Tailwind CSS
- TanStack Query
- React Hook Form + Zod

### Backend

- Node.js with NestJS or Express
- PostgreSQL
- Prisma ORM
- Redis for queues/session/cache
- S3-compatible object storage for documents
- BullMQ for background sync jobs

### Auth and Security

- JWT access token + refresh token
- MFA-ready auth flow
- Role-based access control
- Organization-level data isolation
- Audit logs
- Encryption at rest for sensitive fields
- Signed URLs for document access

## Suggested Database Tables

- organizations
- users
- organization_members
- clients
- client_identifiers
- notices
- notice_documents
- notice_status_events
- notice_comments
- notice_assignments
- reminders
- tasks
- gst_notices
- itr_filings
- sync_jobs
- subscriptions
- invoices
- audit_logs

## MVP Statuses

- New
- Open
- In Progress
- Replied
- Filed
- Closed
- Order Received
- Appeal
- Overdue

## First Build Milestone

Create a working SaaS prototype with:

- Login screen
- Dashboard
- Client list
- Notice list
- Notice detail page
- Upload document UI
- Status update UI
- Comment thread
- Reminder UI

Use mock data first. After the workflow feels right, connect the backend and database.

## Product Positioning

This should not be a clone of any existing product. The aim is to build our own tax-compliance operating system with similar problem coverage:

- Better workflow clarity
- Cleaner task ownership
- Strong audit trail
- Easier notice response management
- AI-ready document and notice intelligence

