
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';

import { HttpClient } from '@angular/common/http';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';

import {
  AuthService,
  CurrentUser
} from '../services/auth.service';

interface Employee {
  id: number;
  employeeNumber: string;
  fullName: string;
  employmentStatus: number | string;
}

interface Department {
  id: number;
  name: string;
}

interface Position {
  id: number;
  title: string;
}

interface Assignment {
  id: number;
  employeeName: string;
}

interface EmployeePage {
  totalCount: number;
  employees: Employee[];
}

interface DepartmentPage {
  totalCount: number;
  departments: Department[];
}

interface PositionPage {
  totalCount: number;
  positions: Position[];
}

interface AssignmentPage {
  totalCount: number;
  assignments: Assignment[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {

  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api';

  currentUser: CurrentUser | null =
    this.authService.getCurrentUser();

  isLoading = true;
  hasLoadError = false;

  stats = {
    totalEmployees: 0,
    activeEmployees: 0,
    departments: 0,
    positions: 0,
    currentAssignments: 0
  };

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {

    this.isLoading = true;
    this.hasLoadError = false;

    forkJoin({

      employees: this.http.get<EmployeePage>(
        `${this.apiUrl}/employees?page=1&pageSize=100`
      ).pipe(
        catchError(error => {
          console.error('Employees API error:', error);
          return of(null);
        })
      ),

      departments: this.http.get<DepartmentPage>(
        `${this.apiUrl}/departments?page=1&pageSize=100`
      ).pipe(
        catchError(error => {
          console.error('Departments API error:', error);
          return of(null);
        })
      ),

      positions: this.http.get<PositionPage>(
        `${this.apiUrl}/positions?page=1&pageSize=100`
      ).pipe(
        catchError(error => {
          console.error('Positions API error:', error);
          return of(null);
        })
      ),

      assignments: this.http.get<AssignmentPage>(
        `${this.apiUrl}/position-assignments?currentOnly=true&page=1&pageSize=100`
      ).pipe(
        catchError(error => {
          console.error('Assignments API error:', error);
          return of(null);
        })
      )

    }).pipe(

      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })

    ).subscribe({

      next: result => {

        const employees = result.employees?.employees ?? [];

        this.stats.totalEmployees =
          result.employees?.totalCount ?? employees.length;

        this.stats.activeEmployees =
          employees.filter(employee =>
            employee.employmentStatus === 1 ||
            String(employee.employmentStatus).toLowerCase() === 'active'
          ).length;

        this.stats.departments =
          result.departments?.totalCount ??
          result.departments?.departments?.length ??
          0;

        this.stats.positions =
          result.positions?.totalCount ??
          result.positions?.positions?.length ??
          0;

        this.stats.currentAssignments =
          result.assignments?.totalCount ??
          result.assignments?.assignments?.length ??
          0;

        this.hasLoadError =
          result.employees === null ||
          result.departments === null ||
          result.positions === null ||
          result.assignments === null;

        console.log('CLAMS Dashboard Statistics:', this.stats);

        this.cdr.detectChanges();
      },

      error: error => {
        console.error('Dashboard loading error:', error);
        this.hasLoadError = true;
        this.cdr.detectChanges();
      }

    });
  }

  get userName(): string {
    return this.currentUser?.employeeName ?? 'Portal User';
  }

  get userRole(): string {
    return this.currentUser?.roles?.[0] ?? 'Portal User';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
