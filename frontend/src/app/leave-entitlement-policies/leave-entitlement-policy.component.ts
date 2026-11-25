import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface LeaveType {
  id: number;
  code: string;
  name: string;
  isPaid: boolean;
  isActive: boolean;
}

interface LeaveTypeResponse {
  totalCount: number;
  leaveTypes: LeaveType[];
}

interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  employmentType: number;
  employmentStatus?: number;
}

interface EmployeeResponse {
  employees: Employee[];
}

interface LeavePolicy {
  id: number;
  leaveTypeId: number;
  leaveTypeCode: string;
  leaveTypeName: string;
  employmentType?: string | null;
  employeeId?: number | null;
  employeeNumber?: string | null;
  employeeName?: string | null;
  annualEntitlementDays: number;
  halfPayEntitlementDays: number;
  accruesMonthly: boolean;
  allowCarryForward: boolean;
  maxCarryForwardDays: number;
  carryForwardExpiryMonths: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

interface PolicyResponse {
  totalCount: number;
  policies: LeavePolicy[];
}

interface PolicyForm {
  leaveTypeId: number | null;
  targetType: 'employment' | 'employee';
  employmentType: number | null;
  employeeId: number | null;
  annualEntitlementDays: number;
  halfPayEntitlementDays: number;
  accruesMonthly: boolean;
  allowCarryForward: boolean;
  maxCarryForwardDays: number;
  carryForwardExpiryMonths: number;
  effectiveFrom: string;
  effectiveTo: string;
}

@Component({
  selector: 'app-leave-entitlement-policy',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-entitlement-policy.component.html',
  styleUrl: './leave-entitlement-policy.component.scss'
})
export class LeaveEntitlementPolicyComponent implements OnInit {

  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl =
    'http://localhost:5219/api/LeaveEntitlementPolicies';

  private readonly leaveTypesUrl =
    'http://localhost:5219/api/LeaveTypes';

  private readonly employeesUrl =
    'http://localhost:5219/api/Employees';

  policies: LeavePolicy[] = [];
  leaveTypes: LeaveType[] = [];
  employees: Employee[] = [];

  isLoading = false;
  isSaving = false;
  hasError = false;

  searchTerm = '';
  selectedStatus = '';
  selectedLeaveType = '';
  selectedTarget = '';

  showForm = false;
  isEditing = false;
  editingId: number | null = null;

  message = '';
  errorMessage = '';

  readonly employmentTypes = [
    { value: 1, label: 'Permanent' },
    { value: 2, label: 'Contract' },
    { value: 3, label: 'Temporary' },
    { value: 4, label: 'Intern' },
    { value: 5, label: 'Casual' }
  ];

  form: PolicyForm = this.emptyForm();

  ngOnInit(): void {
    this.loadReferenceData();
    this.loadPolicies();
  }

  private emptyForm(): PolicyForm {
    return {
      leaveTypeId: null,
      targetType: 'employment',
      employmentType: 1,
      employeeId: null,
      annualEntitlementDays: 21,
      halfPayEntitlementDays: 0,
      accruesMonthly: true,
      allowCarryForward: false,
      maxCarryForwardDays: 10,
      carryForwardExpiryMonths: 18,
      effectiveFrom: new Date().toISOString().slice(0, 10),
      effectiveTo: ''
    };
  }

  loadReferenceData(): void {
    this.http.get<LeaveTypeResponse>(this.leaveTypesUrl)
      .subscribe({
        next: response => {
          this.leaveTypes = (response.leaveTypes ?? [])
            .filter(item => item.isActive && item.isPaid);

          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Leave type reference data error:', error);
          this.errorMessage =
            'Unable to load paid leave types. Please refresh the page.';
          this.cdr.detectChanges();
        }
      });

    this.http.get<EmployeeResponse>(
      `${this.employeesUrl}?page=1&pageSize=100`
    ).subscribe({
      next: response => {
        this.employees = response.employees ?? [];
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Employee reference data error:', error);
        this.errorMessage =
          'Unable to load employees. Please refresh the page.';
        this.cdr.detectChanges();
      }
    });
  }

  loadPolicies(): void {
    this.isLoading = true;
    this.hasError = false;

    this.http.get<PolicyResponse>(this.apiUrl).subscribe({
      next: response => {
        this.policies = response.policies ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave policies loading error:', error);
        this.hasError = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get filteredPolicies(): LeavePolicy[] {
    let results = [...this.policies];

    const search = this.searchTerm.trim().toLowerCase();

    if (search) {
      results = results.filter(item =>
        item.leaveTypeName.toLowerCase().includes(search) ||
        item.leaveTypeCode.toLowerCase().includes(search) ||
        (item.employeeNumber ?? '').toLowerCase().includes(search) ||
        (item.employeeName ?? '').toLowerCase().includes(search) ||
        (item.employmentType ?? '').toLowerCase().includes(search)
      );
    }

    if (this.selectedStatus !== '') {
      const active = this.selectedStatus === 'active';
      results = results.filter(item => item.isActive === active);
    }

    if (this.selectedLeaveType !== '') {
      results = results.filter(
        item => item.leaveTypeId === Number(this.selectedLeaveType)
      );
    }

    if (this.selectedTarget !== '') {
      results = results.filter(item =>
        this.selectedTarget === 'employee'
          ? item.employeeId !== null && item.employeeId !== undefined
          : item.employeeId === null || item.employeeId === undefined
      );
    }

    return results;
  }

  get activeCount(): number {
    return this.policies.filter(item => item.isActive).length;
  }

  get inactiveCount(): number {
    return this.policies.filter(item => !item.isActive).length;
  }

  get employeePolicyCount(): number {
    return this.policies.filter(
      item => item.employeeId !== null && item.employeeId !== undefined
    ).length;
  }

  get employmentPolicyCount(): number {
    return this.policies.filter(
      item => item.employeeId === null || item.employeeId === undefined
    ).length;
  }

  get isAnnualLeaveSelected(): boolean {
    const selected = this.leaveTypes.find(
      item => item.id === Number(this.form.leaveTypeId)
    );

    return selected?.code.toUpperCase() === 'AL';
  }

  get isSickLeaveSelected(): boolean {
    const selected = this.leaveTypes.find(
      item => item.id === Number(this.form.leaveTypeId)
    );

    return selected?.code.toUpperCase() === 'SL';
  }
  get selectedEmployee(): Employee | undefined {
    return this.employees.find(
      item => item.id === Number(this.form.employeeId)
    );
  }

  getEmployeeName(employee: Employee): string {
    return employee.fullName ||
      `${employee.firstName} ${employee.lastName}`;
  }

  getTargetDescription(policy: LeavePolicy): string {
    if (policy.employeeId !== null && policy.employeeId !== undefined) {
      return `${policy.employeeNumber ?? ''} - ${policy.employeeName ?? 'Individual employee'}`;
    }

    return policy.employmentType ?? 'Employment category';
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = '';
    this.selectedLeaveType = '';
    this.selectedTarget = '';
  }

  openAddForm(): void {
    this.form = this.emptyForm();
    this.isEditing = false;
    this.editingId = null;
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  openEditForm(item: LeavePolicy): void {
    const employeeSpecific =
      item.employeeId !== null && item.employeeId !== undefined;

    const employmentTypeValue = this.employmentTypes.find(
      type => type.label.toLowerCase() ===
        (item.employmentType ?? '').toLowerCase()
    )?.value ?? 1;

    this.form = {
      leaveTypeId: item.leaveTypeId,
      targetType: employeeSpecific ? 'employee' : 'employment',
      employmentType: employeeSpecific ? null : employmentTypeValue,
      employeeId: employeeSpecific ? item.employeeId! : null,
      annualEntitlementDays: item.annualEntitlementDays,
      halfPayEntitlementDays: item.halfPayEntitlementDays ?? 0,
      accruesMonthly: item.accruesMonthly,
      allowCarryForward: item.allowCarryForward,
      maxCarryForwardDays: item.maxCarryForwardDays,
      carryForwardExpiryMonths: item.carryForwardExpiryMonths,
      effectiveFrom: item.effectiveFrom,
      effectiveTo: item.effectiveTo ?? ''
    };

    this.isEditing = true;
    this.editingId = item.id;
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.errorMessage = '';
  }

  onTargetTypeChange(): void {
    if (this.form.targetType === 'employment') {
      this.form.employeeId = null;
      this.form.employmentType = 1;
    } else {
      this.form.employmentType = null;
      this.form.employeeId = null;
    }
  }

  onLeaveTypeChange(): void {
    this.form.accruesMonthly = this.isAnnualLeaveSelected;

    if (!this.isAnnualLeaveSelected) {
      this.form.allowCarryForward = false;
    }

    if (!this.isSickLeaveSelected) {
      this.form.halfPayEntitlementDays = 0;
    }
  }

  savePolicy(): void {
    if (!this.form.leaveTypeId) {
      this.errorMessage = 'Please select a paid leave type.';
      return;
    }

    if (
      this.form.targetType === 'employment' &&
      !this.form.employmentType
    ) {
      this.errorMessage = 'Please select an employment category.';
      return;
    }

    if (
      this.form.targetType === 'employee' &&
      !this.form.employeeId
    ) {
      this.errorMessage = 'Please select an employee.';
      return;
    }

    if (
      this.form.annualEntitlementDays < 0 ||
      this.form.annualEntitlementDays > 366
    ) {
      this.errorMessage =
        'Annual entitlement must be between 0 and 366 days.';
      return;
    }

    if (
      this.form.halfPayEntitlementDays < 0 ||
      this.form.halfPayEntitlementDays > 366
    ) {
      this.errorMessage =
        'Half-pay entitlement must be between 0 and 366 days.';
      return;
    }

    if (
      this.form.halfPayEntitlementDays > 0 &&
      !this.isSickLeaveSelected
    ) {
      this.errorMessage =
        'Half-pay entitlement is only permitted for Sick Leave.';
      return;
    }

    if (this.form.accruesMonthly !== this.isAnnualLeaveSelected) {
      this.errorMessage =
        'Monthly accrual must be enabled for Annual Leave only.';
      return;
    }

    if (
      this.form.allowCarryForward &&
      (
        !this.isAnnualLeaveSelected ||
        this.form.maxCarryForwardDays <= 0
      )
    ) {
      this.errorMessage =
        'Carry-forward is available only for Annual Leave and must have a limit greater than zero.';
      return;
    }

    if (
      this.form.maxCarryForwardDays < 0 ||
      this.form.carryForwardExpiryMonths < 1 ||
      this.form.carryForwardExpiryMonths > 120
    ) {
      this.errorMessage =
        'Check the carry-forward limit and expiry period.';
      return;
    }

    if (
      this.form.effectiveTo &&
      this.form.effectiveTo < this.form.effectiveFrom
    ) {
      this.errorMessage =
        'The effective end date cannot be earlier than the start date.';
      return;
    }

    const payload = {
      leaveTypeId: Number(this.form.leaveTypeId),
      employmentType:
        this.form.targetType === 'employment'
          ? Number(this.form.employmentType)
          : null,
      employeeId:
        this.form.targetType === 'employee'
          ? Number(this.form.employeeId)
          : null,
      annualEntitlementDays: Number(this.form.annualEntitlementDays),
      halfPayEntitlementDays: Number(this.form.halfPayEntitlementDays),
      accruesMonthly: this.form.accruesMonthly,
      allowCarryForward: this.form.allowCarryForward,
      maxCarryForwardDays: Number(this.form.maxCarryForwardDays),
      carryForwardExpiryMonths: Number(this.form.carryForwardExpiryMonths),
      effectiveFrom: this.form.effectiveFrom,
      effectiveTo: this.form.effectiveTo || null
    };

    this.isSaving = true;
    this.errorMessage = '';

    const request =
      this.isEditing && this.editingId !== null
        ? this.http.put(
          `${this.apiUrl}/${this.editingId}`,
          payload
        )
        : this.http.post(this.apiUrl, payload);

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;

        this.message = this.isEditing
          ? 'Leave entitlement policy updated successfully.'
          : 'Leave entitlement policy created successfully.';

        this.loadPolicies();
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave policy save error:', error);

        this.isSaving = false;
        this.errorMessage =
          error.error?.message ??
          'Unable to save the policy. Check the details and try again.';

        this.cdr.detectChanges();
      }
    });
  }

  deactivatePolicy(item: LeavePolicy): void {
    if (!confirm(
      `Are you sure you want to deactivate the policy for ${item.leaveTypeName}?`
    )) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${item.id}/deactivate`,
      {}
    ).subscribe({
      next: () => {
        this.message = 'Leave entitlement policy deactivated successfully.';
        this.loadPolicies();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ??
          'Unable to deactivate this policy.';
        this.cdr.detectChanges();
      }
    });
  }

  activatePolicy(item: LeavePolicy): void {
    if (!confirm(
      `Are you sure you want to reactivate the policy for ${item.leaveTypeName}?`
    )) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${item.id}/activate`,
      {}
    ).subscribe({
      next: () => {
        this.message = 'Leave entitlement policy reactivated successfully.';
        this.loadPolicies();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ??
          'Unable to reactivate this policy.';
        this.cdr.detectChanges();
      }
    });
  }
}


