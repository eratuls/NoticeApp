import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface UsageLimits {
  planName: string;
  isActive: boolean;
  startsAtUtc: string;
  expiresAtUtc: string;
  daysRemaining: number;
  assesseeUsed: number;
  assesseeLimit: number;
  assesseeRemaining: number;
  syncCreditsUsed: number;
  syncCreditLimit: number;
  syncCreditsRemaining: number;
  modulesEnabled: string[];
  note: string;
}

@Component({
  selector: 'app-usage-limits',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './usage-limits.component.html',
  styleUrl: './usage-limits.component.scss'
})
export class UsageLimitsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly usage = signal<UsageLimits | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.http.get<UsageLimits>(`${environment.apiBaseUrl}/api/v1/usage`).subscribe({
      next: (res) => {
        this.usage.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load usage and limits.');
        this.loading.set(false);
      }
    });
  }

  pct(used: number, limit: number): number {
    if (limit <= 0) {
      return 0;
    }
    return Math.min(100, Math.round((used / limit) * 100));
  }
}
