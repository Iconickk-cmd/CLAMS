import { formatDate } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { timeout } from 'rxjs';
import { MyProfile, ProfileService } from '../services/profile.service';
import {
  EmployeeTab,
  EmployeeTabsComponent
} from '../shared/employee-tabs.component';

interface Field {
  label: string;
  value: string;
}

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [EmployeeTabsComponent],
  templateUrl: './my-profile.component.html',
  styleUrl: './my-profile.component.scss'
})
export class MyProfileComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  section: EmployeeTab = 'profile';

  profile: MyProfile | null = null;
  isLoading = true;
  errorMessage = '';

  initials = 'U';
  statusClass = '';
  statusText = '';
  headline = '';

  personal: Field[] = [];
  contact: Field[] = [];
  employment: Field[] = [];
  kin: Field[] = [];
  hasNextOfKin = false;

  ngOnInit(): void {
    this.section =
      this.route.snapshot.data['section'] === 'next-of-kin'
        ? 'next-of-kin'
        : 'profile';

    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.profileService.getMyProfile().pipe(timeout(20000)).subscribe({
      next: profile => {
        try {
          this.profile = profile;
          this.buildView(profile);
          this.isLoading = false;
        } catch (e) {
          console.error('Profile display error:', e);
          this.isLoading = false;
          this.errorMessage = 'Your details were received but could not be displayed.';
        }
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Profile loading error:', error);
        this.isLoading = false;
        this.errorMessage = this.describeError(error);
        this.cdr.detectChanges();
      }
    });
  }

  private buildView(p: MyProfile): void {
    const parts = (p.fullName || '').trim().split(' ').filter(Boolean);
    this.initials =
      parts.slice(0, 2).map(x => x.charAt(0).toUpperCase()).join('') || 'U';

    this.statusClass = p.employmentStatus === 'Active' ? 'ok' : 'other';
    this.statusText = this.text(this.humanize(p.employmentStatus));

    this.headline = [p.currentPositionTitle, p.departmentName]
      .filter(Boolean)
      .join(' | ');

    this.personal = [
      { label: 'Full name', value: this.text(p.fullName) },
      { label: 'Gender', value: this.text(this.humanize(p.gender)) },
      { label: 'Date of birth', value: this.date(p.dateOfBirth) },
      { label: 'National ID', value: this.text(p.nationalId) }
    ];

    this.contact = [
      { label: 'Email', value: this.text(p.email) },
      { label: 'Phone number', value: this.text(p.phoneNumber) },
      { label: 'Residential address', value: this.text(p.residentialAddress) }
    ];

    this.employment = [
      { label: 'Employee number', value: this.text(p.employeeNumber) },
      { label: 'Department', value: this.text(p.departmentName) },
      { label: 'Position', value: this.text(p.currentPositionTitle) },
      { label: 'Supervisor', value: this.text(p.supervisorName) },
      { label: 'In position since', value: this.date(p.positionStartDate) },
      { label: 'Date employed', value: this.date(p.dateOfEmployment) },
      { label: 'Length of service', value: this.serviceLength(p.dateOfEmployment) },
      { label: 'Employment type', value: this.text(this.humanize(p.employmentType)) },
      { label: 'Employment status', value: this.text(this.humanize(p.employmentStatus)) }
    ];

    this.kin = [
      { label: 'Name', value: this.text(p.emergencyContactName) },
      { label: 'Relationship', value: this.text(p.emergencyContactRelationship) },
      { label: 'Phone number', value: this.text(p.emergencyContactPhone) }
    ];

    this.hasNextOfKin = !!(
      (p.emergencyContactName && p.emergencyContactName.trim()) ||
      (p.emergencyContactPhone && p.emergencyContactPhone.trim()) ||
      (p.emergencyContactRelationship && p.emergencyContactRelationship.trim())
    );
  }

  private text(value?: string | null): string {
    const v = value === null || value === undefined ? '' : String(value).trim();
    return v ? v : 'Not provided';
  }

  private humanize(value?: string | null): string {
    return value ? value.replace(/([a-z])([A-Z])/g, '$1 $2') : '';
  }

  private date(value?: string | null): string {
    if (!value) {
      return 'Not provided';
    }
    try {
      return formatDate(value.substring(0, 10), 'd MMMM y', 'en-US');
    } catch {
      return 'Not provided';
    }
  }

  private serviceLength(value?: string | null): string {
    if (!value) {
      return 'Not provided';
    }

    const start = new Date(value.substring(0, 10) + 'T00:00:00');
    if (isNaN(start.getTime())) {
      return 'Not provided';
    }

    const now = new Date();
    let months =
      (now.getFullYear() - start.getFullYear()) * 12 +
      (now.getMonth() - start.getMonth());
    if (now.getDate() < start.getDate()) {
      months -= 1;
    }
    if (months < 0) {
      return 'Not started yet';
    }

    const years = Math.floor(months / 12);
    const rest = months % 12;
    const y = years === 1 ? '1 year' : `${years} years`;
    const m = rest === 1 ? '1 month' : `${rest} months`;

    if (years === 0) {
      return m;
    }
    return rest === 0 ? y : `${y}, ${m}`;
  }

  private describeError(error: unknown): string {
    const e = error as {
      status?: number;
      name?: string;
      error?: { message?: string };
    };

    if (e?.name === 'TimeoutError') {
      return 'The server did not respond in time.';
    }
    if (e?.status === 0) {
      return 'The API could not be reached. Is it running?';
    }
    if (e?.status === 404) {
      return e.error?.message ?? 'Your employee record was not found.';
    }
    if (e?.status) {
      return `The server returned an error (HTTP ${e.status}).`;
    }
    return 'Unable to load your details.';
  }
}
