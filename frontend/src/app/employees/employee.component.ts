
import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';

interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  fullName: string;
  nationalId?: string | null;
  gender?: number | null;
  dateOfBirth?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  residentialAddress?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null;
  dateOfEmployment: string;
  employmentType: number;
  employmentStatus: number;
  departmentId: number;
  departmentName: string;
  supervisorEmployeeId?: number | null;
  supervisorName?: string | null;
  currentPositionId?: number | null;
  currentPositionTitle?: string | null;
}

interface EmployeePage {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  employees: Employee[];
}

interface Department {
  id: number;
  name: string;
  isActive: boolean;
}

interface DepartmentPage {
  departments: Department[];
}

interface EmployeeForm {
  firstName: string;
  middleName: string;
  lastName: string;
  nationalId: string;
  gender: number | null;
  dateOfBirth: string;
  email: string;
  phoneNumber: string;
  residentialAddress: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  emergencyContactRelationship: string;
  dateOfEmployment: string;
  employmentType: number;
  departmentId: number | null;
  supervisorEmployeeId: number | null;
}

@Component({
  selector: 'app-employee',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee.component.html',
  styleUrl: './employee.component.scss'
})
export class EmployeeComponent implements OnInit {

  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api';

  employees: Employee[] = [];
  supervisorOptions: Employee[] = [];
  isLoadingSupervisors = false;
  departments: Department[] = [];

  isLoading = false;
  isSaving = false;
  hasError = false;

  searchTerm = '';
  selectedDepartment = '';
  selectedStatus = '';

  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  showForm = false;
  isEditing = false;
  editingEmployeeId: number | null = null;

  // Employee details panel
  showDetails = false;
  isLoadingDetails = false;
  selectedEmployee: Employee | null = null;
  detailsError = '';

  message = '';
  errorMessage = '';

  form: EmployeeForm = this.emptyForm();

  ngOnInit(): void {
    this.loadDepartments();
    this.loadSupervisorOptions();
    this.loadEmployees();
  }

  private emptyForm(): EmployeeForm {
    return {
      firstName: '',
      middleName: '',
      lastName: '',
      nationalId: '',
      gender: null,
      dateOfBirth: '',
      email: '',
      phoneNumber: '',
      residentialAddress: '',
      emergencyContactName: '',
      emergencyContactPhone: '',
      emergencyContactRelationship: '',
      dateOfEmployment: '',
      employmentType: 1,
      departmentId: null,
      supervisorEmployeeId: null
    };
  }

  loadDepartments(): void {
    this.http.get<DepartmentPage>(
      `${this.apiUrl}/departments?page=1&pageSize=100`
    ).subscribe({
      next: response => {
        this.departments = (response.departments ?? [])
          .filter(department => department.isActive);

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Department loading error:', error);
      }
    });
  }

  loadSupervisorOptions(): void {
    this.isLoadingSupervisors = true;
    this.supervisorOptions = [];
    this.loadSupervisorPage(1, [], 1);
  }

  private loadSupervisorPage(
    page: number,
    collected: Employee[],
    totalPages: number
  ): void {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', 100);

    this.http.get<EmployeePage>(
      `${this.apiUrl}/employees`,
      { params }
    ).subscribe({
      next: response => {
        const allEmployees = [
          ...collected,
          ...(response.employees ?? [])
        ];

        const pages = response.totalPages ?? totalPages;

        if (page < pages) {
          this.loadSupervisorPage(page + 1, allEmployees, pages);
          return;
        }

        this.supervisorOptions = allEmployees;
        this.isLoadingSupervisors = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Supervisor options loading error:', error);
        this.isLoadingSupervisors = false;
        this.cdr.detectChanges();
      }
    });
  }
  loadEmployees(): void {
    this.isLoading = true;
    this.hasError = false;

    let params = new HttpParams()
      .set('page', this.page)
      .set('pageSize', this.pageSize);

    if (this.searchTerm.trim()) {
      params = params.set('search', this.searchTerm.trim());
    }

    if (this.selectedDepartment) {
      params = params.set('departmentId', this.selectedDepartment);
    }

    if (this.selectedStatus) {
      params = params.set('employmentStatus', this.selectedStatus);
    }

    this.http.get<EmployeePage>(
      `${this.apiUrl}/employees`,
      { params }
    ).subscribe({
      next: response => {
        this.employees = response.employees ?? [];
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.page = response.page;
        this.isLoading = false;

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Employee loading error:', error);

        this.hasError = true;
        this.isLoading = false;

        this.cdr.detectChanges();
      }
    });
  }

  // VIEW EMPLOYEE DETAILS
  viewEmployee(employee: Employee): void {
    this.showDetails = true;
    this.isLoadingDetails = true;
    this.selectedEmployee = null;
    this.detailsError = '';

    this.http.get<Employee>(
      `${this.apiUrl}/employees/${employee.id}`
    ).subscribe({
      next: response => {
        this.selectedEmployee = response;
        this.isLoadingDetails = false;

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Employee details loading error:', error);

        this.detailsError =
          error.error?.message ??
          'Unable to load employee details. Please try again.';

        this.isLoadingDetails = false;

        this.cdr.detectChanges();
      }
    });
  }

  closeDetails(): void {
    this.showDetails = false;
    this.selectedEmployee = null;
    this.detailsError = '';
  }

  searchEmployees(): void {
    this.page = 1;
    this.loadEmployees();
  }

  applyFilters(): void {
    this.page = 1;
    this.loadEmployees();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedDepartment = '';
    this.selectedStatus = '';
    this.page = 1;

    this.loadEmployees();
  }

  changePage(newPage: number): void {
    if (newPage < 1 || newPage > this.totalPages) {
      return;
    }

    this.page = newPage;
    this.loadEmployees();
  }

  openAddForm(): void {
    this.form = this.emptyForm();
    this.isEditing = false;
    this.editingEmployeeId = null;
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  openEditForm(employee: Employee): void {
    this.form = {
      firstName: employee.firstName,
      middleName: employee.middleName ?? '',
      lastName: employee.lastName,
      nationalId: employee.nationalId ?? '',
      gender: employee.gender ?? null,
      dateOfBirth: employee.dateOfBirth ?? '',
      email: employee.email ?? '',
      phoneNumber: employee.phoneNumber ?? '',
      residentialAddress: employee.residentialAddress ?? '',
      emergencyContactName: employee.emergencyContactName ?? '',
      emergencyContactPhone: employee.emergencyContactPhone ?? '',
      emergencyContactRelationship:
        employee.emergencyContactRelationship ?? '',
      dateOfEmployment: employee.dateOfEmployment,
      employmentType: employee.employmentType,
      departmentId: employee.departmentId,
      supervisorEmployeeId: employee.supervisorEmployeeId ?? null
    };

    this.isEditing = true;
    this.editingEmployeeId = employee.id;
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.errorMessage = '';
  }

  saveEmployee(): void {
    if (
      !this.form.firstName.trim() ||
      !this.form.lastName.trim() ||
      !this.form.dateOfEmployment ||
      !this.form.departmentId ||
      !this.form.employmentType ||
      !this.form.supervisorEmployeeId
    ) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }

    const payload = {
      ...this.form,
      firstName: this.form.firstName.trim(),
      middleName: this.form.middleName.trim() || null,
      lastName: this.form.lastName.trim(),
      nationalId: this.form.nationalId.trim() || null,
      gender: this.form.gender,
      dateOfBirth: this.form.dateOfBirth || null,
      email: this.form.email.trim() || null,
      phoneNumber: this.form.phoneNumber.trim() || null,
      residentialAddress:
        this.form.residentialAddress.trim() || null,
      emergencyContactName:
        this.form.emergencyContactName.trim() || null,
      emergencyContactPhone:
        this.form.emergencyContactPhone.trim() || null,
      emergencyContactRelationship:
        this.form.emergencyContactRelationship.trim() || null
    };

    this.isSaving = true;
    this.errorMessage = '';

    const request =
      this.isEditing && this.editingEmployeeId !== null
        ? this.http.put(
          `${this.apiUrl}/employees/${this.editingEmployeeId}`,
          payload
        )
        : this.http.post(
          `${this.apiUrl}/employees`,
          payload
        );

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;

        this.message = this.isEditing
          ? 'Employee details updated successfully.'
          : 'Employee registered successfully.';

        this.page = 1;
        this.loadEmployees();

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Employee save error:', error);

        this.isSaving = false;

        this.errorMessage =
          error.error?.message ??
          this.getValidationMessage(error.error) ??
          'Unable to save employee. Please check the information and try again.';

        this.cdr.detectChanges();
      }
    });
  }

  private getValidationMessage(error: any): string | null {
    if (!error?.errors) {
      return null;
    }

    const messages = Object.values(error.errors) as string[][];
    return messages.flat().join(' ');
  }

  suspendEmployee(employee: Employee): void {
    if (!confirm(
      `Are you sure you want to suspend ${employee.fullName}?`
    )) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/employees/${employee.id}/suspend`,
      {}
    ).subscribe({
      next: () => {
        this.message = `${employee.fullName} has been suspended.`;
        this.loadEmployees();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ?? 'Unable to suspend this employee.';

        this.cdr.detectChanges();
      }
    });
  }

  reactivateEmployee(employee: Employee): void {
    if (!confirm(
      `Are you sure you want to reactivate ${employee.fullName}?`
    )) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/employees/${employee.id}/reactivate`,
      {}
    ).subscribe({
      next: () => {
        this.message = `${employee.fullName} has been reactivated.`;
        this.loadEmployees();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ?? 'Unable to reactivate this employee.';

        this.cdr.detectChanges();
      }
    });
  }

  getStatusName(status: number): string {
    const statuses: Record<number, string> = {
      1: 'Active',
      2: 'On Leave',
      3: 'Suspended',
      4: 'Separated'
    };

    return statuses[status] ?? 'Unknown';
  }

  getEmploymentTypeName(type: number): string {
    const types: Record<number, string> = {
      1: 'Permanent',
      2: 'Contract',
      3: 'Temporary',
      4: 'Intern',
      5: 'Casual'
    };

    return types[type] ?? 'Unknown';
  }

  getGenderName(gender: number | null | undefined): string {
    const genders: Record<number, string> = {
      1: 'Male',
      2: 'Female',
      3: 'Other',
      4: 'Prefer Not to Say'
    };

    return gender
      ? genders[gender] ?? 'Unknown'
      : 'Not specified';
  }

  get firstRecord(): number {
    return this.totalCount === 0
      ? 0
      : (this.page - 1) * this.pageSize + 1;
  }

  get lastRecord(): number {
    return Math.min(
      this.page * this.pageSize,
      this.totalCount
    );
  }
}
