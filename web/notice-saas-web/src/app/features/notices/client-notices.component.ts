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

@Component({
  selector: 'app-client-notices',
  standalone: true,
  imports: [FormsModule, RouterLink],
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

  readonly tabs = [
    { key: 'Notice', label: 'Notices' },
    { key: 'DirectOrder', label: 'Direct orders' },
    { key: 'Manual', label: 'Manual' },
    { key: 'CaseStatus', label: 'Case status' }
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const clientId = this.route.snapshot.paramMap.get('clientId');
    if (!clientId) {
      this.error.set('Client not found.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
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
        },
        error: () => {
          this.error.set('Could not load notices.');
          this.loading.set(false);
        }
      });
  }

  setKind(kind: string): void {
    this.kind.set(kind);
    this.load();
  }

  countFor(kind: string): number {
    return this.data()?.kindCounts?.[kind] ?? 0;
  }
}
