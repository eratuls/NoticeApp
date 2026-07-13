import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { environment } from '../../../environments/environment';

export interface DashboardSummary {
  module: string;
  period: string;
  clients: { total: number; addedInPeriod: number };
  team: { total: number; active: number; inactive: number };
  notices: { total: number; selfPan: number; otherPan: number };
  tasks: { new: number; ongoing: number; closed: number; overdue: number };
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  readonly user = this.auth.user;
  readonly summary = signal<DashboardSummary | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly module = signal('IncomeTax');
  readonly period = signal('Monthly');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.http
      .get<DashboardSummary>(`${environment.apiBaseUrl}/api/v1/dashboard/summary`, {
        params: { module: this.module(), period: this.period() }
      })
      .subscribe({
        next: (data) => {
          this.summary.set(data);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load dashboard summary.');
          this.loading.set(false);
        }
      });
  }

  setPeriod(period: string): void {
    this.period.set(period);
    this.load();
  }
}
