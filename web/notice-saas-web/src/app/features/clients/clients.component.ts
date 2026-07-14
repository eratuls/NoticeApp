import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ClientListItem {
  id: string;
  name: string;
  pan: string;
  aadhaarMasked: string | null;
  caPan: string | null;
  module: string;
  syncFrequency: string;
  portalUsername: string | null;
  isActive: boolean;
  createdAtUtc: string;
  lastSyncAtUtc: string | null;
  nextSyncAtUtc: string | null;
  noticeCount: number;
  latestSyncStatus: string | null;
  latestSyncError: string | null;
  latestNoticesUpserted: number | null;
}

export interface SyncJobResult {
  id: string;
  clientId: string;
  status: string;
  trigger: string;
  noticesUpserted: number;
  errorMessage: string | null;
  otpRequestedAtUtc: string | null;
}

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './clients.component.html',
  styleUrl: './clients.component.scss'
})
export class ClientsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly clients = signal<ClientListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly showPassword = signal(false);
  readonly syncingId = signal<string | null>(null);
  readonly syncMessage = signal('');
  readonly otpJob = signal<{ clientId: string; clientName: string; jobId: string } | null>(null);
  readonly otpSubmitting = signal(false);
  readonly otpError = signal('');
  readonly deleteTarget = signal<ClientListItem | null>(null);
  readonly deletingId = signal<string | null>(null);
  readonly deleteError = signal('');
  otpCode = '';

  module = 'IncomeTax';
  syncFrequency = 'Weekly';
  portalUsername = '';
  portalPassword = '';

  readonly modules = ['IncomeTax', 'Gst', 'InsightReport'];
  readonly syncOptions = ['Daily', 'Weekly', 'Midweek', 'Fortnightly', 'Monthly'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.http
      .get<ClientListItem[]>(`${environment.apiBaseUrl}/api/v1/clients`, {
        params: {
          module: 'IncomeTax',
          search: this.search().trim()
        }
      })
      .subscribe({
        next: (items) => {
          this.clients.set(items);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load clients.');
          this.loading.set(false);
        }
      });
  }

  openForm(): void {
    this.formError.set('');
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.formError.set('');
  }

  openDelete(client: ClientListItem): void {
    this.deleteError.set('');
    this.deleteTarget.set(client);
  }

  closeDelete(): void {
    this.deleteTarget.set(null);
    this.deleteError.set('');
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) {
      return;
    }

    this.deletingId.set(target.id);
    this.deleteError.set('');
    this.http.delete(`${environment.apiBaseUrl}/api/v1/clients/${target.id}`).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.closeDelete();
        this.syncMessage.set(`Deleted ${target.name}. All notices and related data were removed.`);
        this.load();
      },
      error: (err) => {
        this.deletingId.set(null);
        this.deleteError.set(err?.error?.message ?? 'Unable to delete client.');
      }
    });
  }

  submit(): void {
    this.saving.set(true);
    this.formError.set('');
    this.http
      .post<ClientListItem>(`${environment.apiBaseUrl}/api/v1/clients`, {
        module: this.module,
        syncFrequency: this.syncFrequency,
        portalUsername: this.portalUsername.trim(),
        portalPassword: this.portalPassword
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.closeForm();
          this.portalUsername = '';
          this.portalPassword = '';
          this.syncFrequency = 'Weekly';
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.formError.set(err?.error?.message ?? 'Unable to add client. Check Income Tax credentials.');
        }
      });
  }

  triggerSync(client: ClientListItem): void {
    this.syncingId.set(client.id);
    this.syncMessage.set('');
    this.http
      .post<SyncJobResult>(`${environment.apiBaseUrl}/api/v1/clients/${client.id}/sync`, {})
      .subscribe({
        next: (job) => {
          this.syncingId.set(null);
          if (job.status === 'AwaitingOtp') {
            this.otpCode = '';
            this.otpError.set('');
            this.otpJob.set({ clientId: client.id, clientName: client.name, jobId: job.id });
            this.syncMessage.set(
              `Vault OTP required for ${client.name}. Enter the one-time password from the Income Tax portal.`
            );
          } else if (job.status === 'Succeeded') {
            this.syncMessage.set(
              `Sync succeeded for ${client.name}: ${job.noticesUpserted} notice(s) upserted.`
            );
          } else {
            this.syncMessage.set(job.errorMessage ?? `Sync ${job.status} for ${client.name}.`);
          }
          this.load();
        },
        error: (err) => {
          this.syncingId.set(null);
          this.syncMessage.set(err?.error?.message ?? `Unable to sync ${client.name}.`);
        }
      });
  }

  closeOtpModal(): void {
    this.otpJob.set(null);
    this.otpError.set('');
    this.otpCode = '';
  }

  submitOtp(): void {
    const pending = this.otpJob();
    if (!pending) {
      return;
    }

    this.otpSubmitting.set(true);
    this.otpError.set('');
    this.http
      .post<SyncJobResult>(
        `${environment.apiBaseUrl}/api/v1/clients/${pending.clientId}/sync/${pending.jobId}/otp`,
        { otp: this.otpCode.trim() }
      )
      .subscribe({
        next: (job) => {
          this.otpSubmitting.set(false);
          if (job.status === 'Succeeded') {
            this.closeOtpModal();
            this.syncMessage.set(
              `Sync succeeded for ${pending.clientName}: ${job.noticesUpserted} notice(s) upserted.`
            );
          } else if (job.status === 'AwaitingOtp') {
            this.otpError.set('OTP was not accepted. Try again.');
          } else {
            this.closeOtpModal();
            this.syncMessage.set(
              job.errorMessage ?? `Sync ${job.status} for ${pending.clientName}.`
            );
          }
          this.load();
        },
        error: (err) => {
          this.otpSubmitting.set(false);
          this.otpError.set(err?.error?.message ?? 'Unable to submit OTP.');
        }
      });
  }
}
