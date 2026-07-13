import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

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
  comments: { id: string; authorName: string; body: string; createdAtUtc: string }[];
  timeline: { id: string; fromStatus: string | null; toStatus: string; note: string | null; createdAtUtc: string }[];
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

  readonly statuses = ['New', 'Open', 'InProgress', 'Replied', 'Closed'];

  ngOnInit(): void {
    this.load();
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
}
