import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface MasterItem {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc?: string;
  description?: string | null;
  isSystem?: boolean;
}

type MasterTab = 'departments' | 'designations' | 'roles';

@Component({
  selector: 'app-master-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './master-settings.component.html',
  styleUrl: './master-settings.component.scss'
})
export class MasterSettingsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly tab = signal<MasterTab>('departments');
  readonly items = signal<MasterItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly showForm = signal(false);
  readonly editing = signal<MasterItem | null>(null);

  name = '';
  isActive = true;

  ngOnInit(): void {
    this.load();
  }

  setTab(tab: MasterTab): void {
    this.tab.set(tab);
    this.closeForm();
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const path =
      this.tab() === 'departments'
        ? 'departments'
        : this.tab() === 'designations'
          ? 'designations'
          : 'roles';

    this.http
      .get<MasterItem[]>(`${environment.apiBaseUrl}/api/v1/master/${path}`)
      .subscribe({
        next: (res) => {
          this.items.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load master data.');
          this.loading.set(false);
        }
      });
  }

  openCreate(): void {
    if (this.tab() === 'roles') {
      return;
    }
    this.editing.set(null);
    this.name = '';
    this.isActive = true;
    this.formError.set('');
    this.showForm.set(true);
  }

  openEdit(item: MasterItem): void {
    if (this.tab() === 'roles') {
      return;
    }
    this.editing.set(item);
    this.name = item.name;
    this.isActive = item.isActive;
    this.formError.set('');
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editing.set(null);
    this.formError.set('');
  }

  save(): void {
    if (this.tab() === 'roles') {
      return;
    }

    this.saving.set(true);
    this.formError.set('');
    const path = this.tab() === 'departments' ? 'departments' : 'designations';
    const editing = this.editing();

    const request$ = editing
      ? this.http.put(`${environment.apiBaseUrl}/api/v1/master/${path}/${editing.id}`, {
          name: this.name,
          isActive: this.isActive
        })
      : this.http.post(`${environment.apiBaseUrl}/api/v1/master/${path}`, {
          name: this.name
        });

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeForm();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(err?.error?.message ?? 'Unable to save.');
      }
    });
  }

  remove(item: MasterItem): void {
    if (this.tab() === 'roles') {
      return;
    }
    const path = this.tab() === 'departments' ? 'departments' : 'designations';
    this.http.delete(`${environment.apiBaseUrl}/api/v1/master/${path}/${item.id}`).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Unable to delete item.')
    });
  }
}
