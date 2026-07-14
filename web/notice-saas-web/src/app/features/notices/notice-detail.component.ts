import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface NoticeAttachment {
  id: string;
  category: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByName: string;
  createdAtUtc: string;
  downloadUrl: string;
}

interface NoticeDetail {
  id: string;
  clientId: string;
  clientName: string;
  clientPan: string;
  section: string;
  description: string;
  financialYear: string | null;
  proceedingId: string | null;
  documentReferenceId: string | null;
  status: string;
  isOverdue: boolean;
  servedDate: string | null;
  responseDueDate: string | null;
  responseSubmittedDate: string | null;
  assignedToUserId: string | null;
  assignedToName: string | null;
  comments: { id: string; authorName: string; body: string; createdAtUtc: string }[];
  timeline: { id: string; fromStatus: string | null; toStatus: string; note: string | null; createdAtUtc: string }[];
  attachments: NoticeAttachment[];
}

interface TeamMemberOption {
  userId: string;
  firstName: string;
  lastName: string;
}

@Component({
  selector: 'app-notice-detail',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './notice-detail.component.html',
  styleUrl: './notice-detail.component.scss'
})
export class NoticeDetailComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  readonly notice = signal<NoticeDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly commentBody = signal('');
  readonly status = signal('Open');
  readonly reminderDue = signal('');
  readonly reminderPriority = signal('Medium');
  readonly reminderNote = signal('');
  readonly reminderSaving = signal(false);
  readonly reminderMessage = signal('');
  readonly members = signal<TeamMemberOption[]>([]);
  readonly assigneeId = signal('');
  readonly assigning = signal(false);
  readonly uploadMessage = signal('');
  readonly uploading = signal(false);

  readonly statuses = ['New', 'Open', 'InProgress', 'Replied', 'Closed'];
  readonly priorities = ['High', 'Medium', 'Low'];

  ngOnInit(): void {
    this.loadMembers();
    this.load();
  }

  loadMembers(): void {
    this.http.get<{ members: TeamMemberOption[] }>(`${environment.apiBaseUrl}/api/v1/team`).subscribe({
      next: (res) => this.members.set(res.members ?? [])
    });
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('noticeId');
    if (!id) {
      this.error.set('Notice not found.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.http.get<NoticeDetail>(`${environment.apiBaseUrl}/api/v1/notices/${id}`).subscribe({
      next: (n) => {
        this.notice.set(n);
        this.status.set(n.status);
        this.assigneeId.set(n.assignedToUserId ?? '');
        if (!this.reminderDue()) {
          this.reminderDue.set(n.responseDueDate || new Date().toISOString().slice(0, 10));
        }
        if (!this.reminderNote()) {
          this.reminderNote.set(n.description);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load notice.');
        this.loading.set(false);
      }
    });
  }

  updateStatus(): void {
    const n = this.notice();
    if (!n) {
      return;
    }
    this.saving.set(true);
    this.http
      .patch<NoticeDetail>(`${environment.apiBaseUrl}/api/v1/notices/${n.id}/status`, {
        status: this.status()
      })
      .subscribe({
        next: (updated) => {
          this.notice.set(updated);
          this.saving.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.error.set('Unable to update status.');
        }
      });
  }

  assign(): void {
    const n = this.notice();
    if (!n) {
      return;
    }
    this.assigning.set(true);
    this.http
      .patch<NoticeDetail>(`${environment.apiBaseUrl}/api/v1/notices/${n.id}/assign`, {
        assignedToUserId: this.assigneeId() || null
      })
      .subscribe({
        next: (updated) => {
          this.notice.set(updated);
          this.assigneeId.set(updated.assignedToUserId ?? '');
          this.assigning.set(false);
        },
        error: () => {
          this.assigning.set(false);
          this.error.set('Unable to assign notice.');
        }
      });
  }

  addComment(): void {
    const n = this.notice();
    const body = this.commentBody().trim();
    if (!n || !body) {
      return;
    }
    this.http
      .post(`${environment.apiBaseUrl}/api/v1/notices/${n.id}/comments`, { body })
      .subscribe({
        next: () => {
          this.commentBody.set('');
          this.load();
        },
        error: () => this.error.set('Unable to add comment.')
      });
  }

  setReminder(): void {
    const n = this.notice();
    const description = this.reminderNote().trim();
    const dueOn = this.reminderDue();
    if (!n || !description || !dueOn) {
      return;
    }

    this.reminderSaving.set(true);
    this.reminderMessage.set('');
    this.http
      .post(`${environment.apiBaseUrl}/api/v1/reminders`, {
        noticeId: n.id,
        description,
        priority: this.reminderPriority(),
        dueOn
      })
      .subscribe({
        next: () => {
          this.reminderSaving.set(false);
          this.reminderMessage.set('Reminder scheduled.');
        },
        error: () => {
          this.reminderSaving.set(false);
          this.reminderMessage.set('Unable to schedule reminder.');
        }
      });
  }

  downloadUrl(a: NoticeAttachment): string {
    return `${environment.apiBaseUrl}${a.downloadUrl}`;
  }

  onFileSelected(event: Event, category: 'NoticeDocument' | 'Reply'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const n = this.notice();
    if (!file || !n) {
      return;
    }

    const form = new FormData();
    form.append('category', category);
    form.append('file', file);

    this.uploading.set(true);
    this.uploadMessage.set('');
    this.http.post(`${environment.apiBaseUrl}/api/v1/notices/${n.id}/attachments`, form).subscribe({
      next: () => {
        this.uploading.set(false);
        this.uploadMessage.set(`${category === 'Reply' ? 'Reply' : 'Notice'} document uploaded.`);
        input.value = '';
        this.load();
      },
      error: () => {
        this.uploading.set(false);
        this.uploadMessage.set('Upload failed.');
      }
    });
  }
}
