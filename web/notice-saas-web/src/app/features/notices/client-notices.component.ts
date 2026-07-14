import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface NoticeListItem {
  id: string;
  section: string;
  description: string;
  financialYear: string | null;
  documentReferenceId: string | null;
  status: string;
  isOverdue: boolean;
  servedDate: string | null;
  responseDueDate: string | null;
}

interface ClientNoticesResponse {
  clientId: string;
  clientName: string;
  clientPan: string;
  isActive: boolean;
  kindCounts: Record<string, number>;
  notices: NoticeListItem[];
}

interface SyncJobResult {
  id: string;
  clientId: string;
  status: string;
  trigger: string;
  noticesUpserted: number;
  errorMessage: string | null;
  completedAtUtc: string | null;
  otpRequestedAtUtc: string | null;
}

@Component({
  selector: 'app-client-notices',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './client-notices.component.html',
  styleUrl: './client-notices.component.scss'
})
export class ClientNoticesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  readonly data = signal<ClientNoticesResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly kind = signal('Notice');
  readonly search = signal('');
  readonly latestSync = signal<SyncJobResult | null>(null);
  readonly syncing = signal(false);
  readonly syncMessage = signal('');
  readonly otpJobId = signal<string | null>(null);
  readonly otpSubmitting = signal(false);
  readonly otpError = signal('');
  otpCode = '';

  readonly tabs = [
    { key: 'Notice', label: 'Notices' },
    { key: 'DirectOrder', label: 'Direct orders' },
    { key: 'Manual', label: 'Manual' },
    { key: 'CaseStatus', label: 'Case status' }
  ];

  ngOnInit(): void {
    this.load();
  }

  private clientId(): string | null {
    return this.route.snapshot.paramMap.get('clientId');
  }

  load(): void {
    const clientId = this.clientId();
    if (!clientId) {
      this.error.set('Client not found.');
      this.loading.set(false);
      return;
    }

    if (!this.data()) {
      this.loading.set(true);
    }
    this.error.set('');
    this.http
      .get<ClientNoticesResponse>(`${environment.apiBaseUrl}/api/v1/clients/${clientId}/notices`, {
        params: {
          kind: this.kind(),
          search: this.search().trim()
        }
      })
      .subscribe({
        next: (res) => {
          this.data.set(res);
          this.loading.set(false);
          this.loadLatestSync(clientId);
        },
        error: () => {
          this.error.set('Could not load notices.');
          this.loading.set(false);
        }
      });
  }

  private loadLatestSync(clientId: string): void {
    this.http.get<SyncJobResult>(`${environment.apiBaseUrl}/api/v1/clients/${clientId}/sync`).subscribe({
      next: (job) => this.latestSync.set(job),
      error: () => this.latestSync.set(null)
    });
  }

  setKind(kind: string): void {
    this.kind.set(kind);
    this.load();
  }

  countFor(kind: string): number {
    return this.data()?.kindCounts?.[kind] ?? 0;
  }

  triggerSync(): void {
    const clientId = this.clientId();
    const client = this.data();
    if (!clientId || !client) {
      return;
    }

    this.syncing.set(true);
    this.syncMessage.set('');
    this.http
      .post<SyncJobResult>(`${environment.apiBaseUrl}/api/v1/clients/${clientId}/sync`, {})
      .subscribe({
        next: (job) => {
          this.syncing.set(false);
          this.latestSync.set(job);
          if (job.status === 'AwaitingOtp') {
            this.otpCode = '';
            this.otpError.set('');
            this.otpJobId.set(job.id);
            this.syncMessage.set(
              'Vault OTP required. Enter the one-time password from the Income Tax portal.'
            );
          } else if (job.status === 'Succeeded') {
            this.syncMessage.set(`Sync succeeded: ${job.noticesUpserted} notice(s) upserted.`);
            this.load();
          } else {
            this.syncMessage.set(job.errorMessage ?? `Sync ${job.status}.`);
          }
        },
        error: (err) => {
          this.syncing.set(false);
          this.syncMessage.set(err?.error?.message ?? `Unable to sync ${client.clientName}.`);
        }
      });
  }

  closeOtpModal(): void {
    this.otpJobId.set(null);
    this.otpError.set('');
    this.otpCode = '';
  }

  submitOtp(): void {
    const clientId = this.clientId();
    const jobId = this.otpJobId();
    if (!clientId || !jobId) {
      return;
    }

    this.otpSubmitting.set(true);
    this.otpError.set('');
    this.http
      .post<SyncJobResult>(`${environment.apiBaseUrl}/api/v1/clients/${clientId}/sync/${jobId}/otp`, {
        otp: this.otpCode.trim()
      })
      .subscribe({
        next: (job) => {
          this.otpSubmitting.set(false);
          this.latestSync.set(job);
          if (job.status === 'Succeeded') {
            this.closeOtpModal();
            this.syncMessage.set(`Sync succeeded: ${job.noticesUpserted} notice(s) upserted.`);
            this.load();
          } else if (job.status === 'AwaitingOtp') {
            this.otpError.set('OTP was not accepted. Try again.');
          } else {
            this.closeOtpModal();
            this.syncMessage.set(job.errorMessage ?? `Sync ${job.status}.`);
          }
        },
        error: (err) => {
          this.otpSubmitting.set(false);
          this.otpError.set(err?.error?.message ?? 'Unable to submit OTP.');
        }
      });
  }
}
