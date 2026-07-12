import { Component, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

type HealthResponse = {
  status: string;
  service: string;
  utc: string;
};

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  readonly title = 'NoticeSaaS';
  readonly apiStatus = signal<'checking' | 'ok' | 'error'>('checking');
  readonly detail = signal<string>('');

  constructor(private readonly http: HttpClient) {}

  ngOnInit(): void {
    this.checkApi();
  }

  checkApi(): void {
    this.apiStatus.set('checking');
    this.detail.set('');

    this.http.get<HealthResponse>(`${environment.apiBaseUrl}/api/health`).subscribe({
      next: (res) => {
        this.apiStatus.set('ok');
        this.detail.set(`${res.service} · ${res.utc}`);
      },
      error: (err: unknown) => {
        this.apiStatus.set('error');
        const message = err instanceof Error ? err.message : 'Cannot reach API';
        this.detail.set(message);
      }
    });
  }
}
