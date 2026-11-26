import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface MyProfile {
  id: number;
  employeeNumber: string;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  fullName: string;
  nationalId?: string | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  residentialAddress?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null;
  dateOfEmployment: string;
  employmentType: string;
  employmentStatus: string;
  departmentName: string;
  currentPositionTitle?: string | null;
  supervisorName?: string | null;
  positionStartDate?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5219/api';

  getMyProfile(): Observable<MyProfile> {
    return this.http.get<MyProfile>(`${this.apiUrl}/Profile/me`);
  }
}
