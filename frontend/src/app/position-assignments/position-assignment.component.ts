import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';

interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  fullName: string;
  departmentId: number;
  departmentName: string;
  employmentStatus: string | number;
  currentPositionId?: number | null;
  currentPositionTitle?: string | null;
}

interface EmployeePage {
  employees: Employee[];
  totalCount: number;
}

interface Position {
  id: number;
  code: string;
  title: string;
  departmentId: number;
  departmentName: string;
  grade?: string;
  approvedHeadcount: number;
  occupiedHeadcount: number;
  vacantHeadcount: number;
  isActive: boolean;
}

interface PositionPage {
  positions: Position[];
  totalCount: number;
}

interface Assignment {
  id: number;
  employeeId: number;
  employeeNumber: string;
  employeeName: string;
  positionId: number;
  positionCode: string;
  positionTitle: string;
  departmentId: number;
  departmentName: string;
  startDate: string;
  endDate: string | null;
  status: string;
  changeReason: string | null;
  isCurrent: boolean;
  createdAt: string;
}

interface AssignmentPage {
  assignments: Assignment[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

type ModalMode = 'assign' | 'transfer' | 'end' | null;

@Component({
  selector: 'app-position-assignment',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './position-assignment.component.html',
  styleUrl: './position-assignment.component.scss'
})
export class PositionAssignmentComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api';

  assignments: Assignment[] = [];
  employees: Employee[] = [];
  positions: Position[] = [];

  loading = false;
  saving = false;

  searchTerm = '';
  statusFilter = 'all';

  modalMode: ModalMode = null;
  selectedAssignment: Assignment | null = null;

  message = '';
  errorMessage = '';

  form = {
    employeeId: '',
    positionId: '',
    startDate: this.today(),
    changeReason: '',
    endDate: this.today(),
    endReason: ''
  };

  ngOnInit(): void {
    this.loadEmployees();
    this.loadPositions();
    this.loadAssignments();
  }

  today(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  loadAssignments(): void {
    this.loading = true;
    this.errorMessage = '';

    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 100);

    this.http.get<AssignmentPage>(
      `${this.apiUrl}/position-assignments`,
      { params }
    ).subscribe({
      next: response => {
        this.assignments = response.assignments ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.loading = false;
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to load position assignments.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  loadEmployees(): void {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 100);

    this.http.get<EmployeePage>(
      `${this.apiUrl}/employees`,
      { params }
    ).subscribe({
      next: response => {
        this.employees = response.employees ?? [];
        this.cdr.detectChanges();
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to load employees.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  loadPositions(): void {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 100)
      .set('isActive', true);

    this.http.get<PositionPage>(
      `${this.apiUrl}/positions`,
      { params }
    ).subscribe({
      next: response => {
        this.positions = (response.positions ?? [])
          .filter(position => position.isActive);
        this.cdr.detectChanges();
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to load positions.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  get filteredAssignments(): Assignment[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.assignments.filter(assignment => {
      const matchesSearch = !term ||
        assignment.employeeName.toLowerCase().includes(term) ||
        assignment.employeeNumber.toLowerCase().includes(term) ||
        assignment.positionTitle.toLowerCase().includes(term) ||
        assignment.positionCode.toLowerCase().includes(term) ||
        assignment.departmentName.toLowerCase().includes(term);

      const matchesStatus = this.statusFilter === 'all' ||
        assignment.status.toLowerCase() === this.statusFilter.toLowerCase();

      return matchesSearch && matchesStatus;
    });
  }

  get currentCount(): number {
    return this.assignments.filter(a => a.status === 'Current').length;
  }

  get scheduledCount(): number {
    return this.assignments.filter(a => a.status === 'Scheduled').length;
  }

  get completedCount(): number {
    return this.assignments.filter(a => a.status === 'Completed').length;
  }

  get vacantPosts(): number {
    return this.positions.reduce(
      (total, position) => total + (position.vacantHeadcount || 0),
      0
    );
  }

  get activeEmployeesWithoutAssignment(): Employee[] {
    return this.employees.filter(employee => {
      const activeStatus =
        String(employee.employmentStatus).toLowerCase() === 'active' ||
        employee.employmentStatus === 1;

      const hasAssignment = this.assignments.some(assignment =>
        assignment.employeeId === employee.id &&
        (assignment.status === 'Current' ||
          assignment.status === 'Scheduled')
      );

      return activeStatus &&
        !hasAssignment &&
        employee.currentPositionId == null;
    });
  }

  get availablePositions(): Position[] {
    return this.positions.filter(position =>
      position.isActive && position.vacantHeadcount > 0
    );
  }

  get assignablePositions(): Position[] {
    if (!this.form.employeeId) {
      return this.availablePositions;
    }

    const employee = this.employees.find(
      e => e.id === Number(this.form.employeeId)
    );

    if (!employee) {
      return [];
    }

    return this.availablePositions.filter(
      position => position.departmentId === employee.departmentId
    );
  }

  get transferPositions(): Position[] {
    if (!this.selectedAssignment) {
      return [];
    }

    return this.availablePositions.filter(
      position =>
        position.id !== this.selectedAssignment!.positionId
    );
  }

  openAssignModal(): void {
    this.clearMessages();
    this.selectedAssignment = null;
    this.modalMode = 'assign';

    this.form = {
      employeeId: '',
      positionId: '',
      startDate: this.today(),
      changeReason: '',
      endDate: this.today(),
      endReason: ''
    };
  }

  openTransferModal(assignment: Assignment): void {
    this.clearMessages();
    this.selectedAssignment = assignment;
    this.modalMode = 'transfer';

    this.form = {
      employeeId: String(assignment.employeeId),
      positionId: '',
      startDate: this.today(),
      changeReason: '',
      endDate: this.today(),
      endReason: ''
    };
  }

  openEndModal(assignment: Assignment): void {
    this.clearMessages();
    this.selectedAssignment = assignment;
    this.modalMode = 'end';

    this.form = {
      employeeId: String(assignment.employeeId),
      positionId: '',
      startDate: this.today(),
      changeReason: '',
      endDate: this.today(),
      endReason: ''
    };
  }

  closeModal(): void {
    if (this.saving) {
      return;
    }

    this.modalMode = null;
    this.selectedAssignment = null;
    this.clearMessages();
  }

  onEmployeeChange(): void {
    this.form.positionId = '';
  }

  saveAssignment(): void {
    if (this.saving) {
      return;
    }

    if (this.modalMode === 'assign') {
      this.createAssignment();
    } else if (this.modalMode === 'transfer') {
      this.transferAssignment();
    } else if (this.modalMode === 'end') {
      this.endAssignment();
    }
  }

  private createAssignment(): void {
    if (
      !this.form.employeeId ||
      !this.form.positionId ||
      !this.form.startDate
    ) {
      this.errorMessage = 'Select an employee, position and start date.';
      return;
    }

    const payload = {
      employeeId: Number(this.form.employeeId),
      positionId: Number(this.form.positionId),
      startDate: this.form.startDate,
      changeReason: this.form.changeReason.trim() || null
    };

    this.saving = true;
    this.clearMessages();

    this.http.post(
      `${this.apiUrl}/position-assignments`,
      payload
    ).subscribe({
      next: () => {
        this.saving = false;
        this.modalMode = null;
        this.message = 'Employee assigned to the position successfully.';
        this.refreshData();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to create the position assignment.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  private transferAssignment(): void {
    if (
      !this.selectedAssignment ||
      !this.form.positionId ||
      !this.form.startDate ||
      !this.form.changeReason.trim()
    ) {
      this.errorMessage =
        'Select a destination position, transfer date and reason.';
      return;
    }

    const payload = {
      newPositionId: Number(this.form.positionId),
      startDate: this.form.startDate,
      changeReason: this.form.changeReason.trim()
    };

    this.saving = true;
    this.clearMessages();

    this.http.post(
      `${this.apiUrl}/position-assignments/${this.selectedAssignment.id}/transfer`,
      payload
    ).subscribe({
      next: () => {
        this.saving = false;
        this.modalMode = null;
        this.message = 'Employee transfer processed successfully.';
        this.refreshData();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to transfer the employee.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  private endAssignment(): void {
    if (
      !this.selectedAssignment ||
      !this.form.endDate ||
      !this.form.endReason.trim()
    ) {
      this.errorMessage = 'Enter an end date and reason.';
      return;
    }

    const payload = {
      endDate: this.form.endDate,
      reason: this.form.endReason.trim()
    };

    this.saving = true;
    this.clearMessages();

    this.http.patch(
      `${this.apiUrl}/position-assignments/${this.selectedAssignment.id}/end`,
      payload
    ).subscribe({
      next: () => {
        this.saving = false;
        this.modalMode = null;
        this.message = 'Position assignment ended successfully.';
        this.refreshData();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to end the position assignment.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  refreshData(): void {
    this.loadAssignments();
    this.loadEmployees();
    this.loadPositions();
    this.cdr.detectChanges();
  }

  refresh(): void {
    this.clearMessages();
    this.refreshData();
  }

  clearMessages(): void {
    this.message = '';
    this.errorMessage = '';
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'current':
        return 'status-current';
      case 'scheduled':
        return 'status-scheduled';
      case 'completed':
        return 'status-completed';
      default:
        return '';
    }
  }

  getModalTitle(): string {
    switch (this.modalMode) {
      case 'assign':
        return 'Assign Employee to Position';
      case 'transfer':
        return 'Transfer Employee';
      case 'end':
        return 'End Position Assignment';
      default:
        return '';
    }
  }

  getErrorMessage(error: any, fallback: string): string {
    return error?.error?.message ||
      error?.error?.title ||
      error?.message ||
      fallback;
  }
}
