import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface ReminderItem {
  id: string;
  noticeId: string | null;
  description: string;
  proceedingId: string | null;
  documentReferenceId: string | null;
  assesseeIdentifier: string | null;
  priority: string;
  dueOn: string;
  isDone: boolean;
  isOverdue: boolean;
  module: string;
}

interface ReminderListResponse {
  pendingCount: number;
  doneCount: number;
  reminders: ReminderItem[];
}

@Component({
  selector: 'app-reminders',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './reminders.component.html',
  styleUrl: './reminders.component.scss'
})
export class RemindersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly data = signal<ReminderListResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly status = signal<'Pending' | 'Done'>('Pending');
  readonly search = signal('');
  readonly priority = signal('');

  readonly priorities = ['', 'High', 'Medium', 'Low'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const params: Record<string, string> = {
      status: this.status()
    };
    if (this.search().trim()) {
      params['search'] = this.search().trim();
    }
    if (this.priority()) {
      params['priority'] = this.priority();
    }

    this.http
      .get<ReminderListResponse>(`${environment.apiBaseUrl}/api/v1/reminders`, { params })
      .subscribe({
        next: (res) => {
          this.data.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load reminders.');
          this.loading.set(false);
        }
      });
  }

  setStatus(status: 'Pending' | 'Done'): void {
    this.status.set(status);
    this.load();
  }

  complete(id: string): void {
    this.http.post(`${environment.apiBaseUrl}/api/v1/reminders/${id}/complete`, {}).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Unable to complete reminder.')
    });
  }
}
