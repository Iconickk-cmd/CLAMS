import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

interface EmployeeProfile {
  id: number;
  employeeNumber: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  fullName: string;
  nationalId: string | null;
  gender: string | null;
  dateOfBirth: string | null;
  email: string | null;
  phoneNumber: string | null;
  residentialAddress: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  emergencyContactRelationship: string | null;
  dateOfEmployment: string;
  employmentType: string;
  employmentStatus: string;
  departmentId: number;
  departmentName: string;
  supervisorName: string | null;
  currentPosition: {
    positionId: number | null;
    code: string;
    title: string;
    grade: string | null;
    positionDepartment: string;
  } | null;
  createdAt: string;
  updatedAt: string | null;
}

interface NextOfKinContact {
  id: number;
  fullName: string;
  relationship: string;
  phoneNumber: string;
  alternativePhoneNumber: string | null;
  email: string | null;
  physicalAddress: string | null;
}

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './employee-profile.component.html',
  styleUrl: './employee-profile.component.scss'
})
export class EmployeeProfileComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api';

  profile: EmployeeProfile | null = null;
  nextOfKinContacts: NextOfKinContact[] = [];
  activeTab = 'basic';
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadProfile();
    this.loadNextOfKin();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.http.get<EmployeeProfile>(
      `${this.apiUrl}/employee-profile/me`
    ).subscribe({
      next: response => {
        this.profile = response;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ||
          'Unable to load your employee profile. Please try again.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadNextOfKin(): void {
    this.http.get<NextOfKinContact[]>(`${this.apiUrl}/employee-profile/me/next-of-kin`).subscribe({
      next: response => {
        this.nextOfKinContacts = response ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.nextOfKinContacts = [];
        this.cdr.detectChanges();
      }
    });
  }

  selectTab(tab: string): void {
    this.activeTab = tab;
  }

  displayValue(value: string | null | undefined): string {
    return value?.trim() || 'Not provided';
  }
}
