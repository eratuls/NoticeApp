import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface TeamMember {
  membershipId: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  role: string;
  department: string | null;
  designation: string | null;
  isActive: boolean;
  joinedAtUtc: string;
}

interface TeamListResponse {
  total: number;
  active: number;
  inactive: number;
  members: TeamMember[];
}

interface NamedOption {
  id: string;
  name: string;
  isActive?: boolean;
}

interface AddMemberResponse {
  member: TeamMember;
  temporaryPassword: string;
}

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './team.component.html',
  styleUrl: './team.component.scss'
})
export class TeamComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly data = signal<TeamListResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly roleFilter = signal('');
  readonly search = signal('');
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal('');
  readonly createdPassword = signal('');

  readonly roles = signal<NamedOption[]>([]);
  readonly departments = signal<NamedOption[]>([]);
  readonly designations = signal<NamedOption[]>([]);

  firstName = '';
  lastName = '';
  email = '';
  phoneNumber = '';
  roleId = '';
  departmentId = '';
  designationId = '';

  ngOnInit(): void {
    this.loadLookups();
    this.load();
  }

  loadLookups(): void {
    this.http
      .get<NamedOption[]>(`${environment.apiBaseUrl}/api/v1/master/roles`)
      .subscribe({ next: (roles) => this.roles.set(roles.filter((r) => r.name !== 'Owner')) });
    this.http
      .get<NamedOption[]>(`${environment.apiBaseUrl}/api/v1/master/departments?activeOnly=true`)
      .subscribe({ next: (d) => this.departments.set(d) });
    this.http
      .get<NamedOption[]>(`${environment.apiBaseUrl}/api/v1/master/designations?activeOnly=true`)
      .subscribe({ next: (d) => this.designations.set(d) });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const params: Record<string, string> = {};
    if (this.roleFilter()) {
      params['role'] = this.roleFilter();
    }
    if (this.search().trim()) {
      params['search'] = this.search().trim();
    }

    this.http
      .get<TeamListResponse>(`${environment.apiBaseUrl}/api/v1/team`, { params })
      .subscribe({
        next: (res) => {
          this.data.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load team.');
          this.loading.set(false);
        }
      });
  }

  setRole(role: string): void {
    this.roleFilter.set(role);
    this.load();
  }

  openForm(): void {
    this.showForm.set(true);
    this.formError.set('');
    this.createdPassword.set('');
    this.firstName = '';
    this.lastName = '';
    this.email = '';
    this.phoneNumber = '';
    this.roleId = this.roles().find((r) => r.name === 'Staff')?.id ?? this.roles()[0]?.id ?? '';
    this.departmentId = this.departments().find((d) => d.name === 'Income Tax')?.id ?? '';
    this.designationId = this.designations()[0]?.id ?? '';
  }

  closeForm(): void {
    this.showForm.set(false);
    this.createdPassword.set('');
  }

  submit(): void {
    this.saving.set(true);
    this.formError.set('');
    this.createdPassword.set('');
    this.http
      .post<AddMemberResponse>(`${environment.apiBaseUrl}/api/v1/team`, {
        firstName: this.firstName,
        lastName: this.lastName,
        email: this.email,
        phoneNumber: this.phoneNumber || null,
        roleId: this.roleId,
        departmentId: this.departmentId || null,
        designationId: this.designationId || null
      })
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          this.createdPassword.set(res.temporaryPassword);
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.formError.set(err?.error?.message ?? 'Unable to add team member.');
        }
      });
  }
}
