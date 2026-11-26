import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface NextOfKinContact {
  id: number;
  fullName: string;
  relationship: string;
  phoneNumber: string;
  alternativePhoneNumber: string | null;
  email: string | null;
  physicalAddress: string | null;
  createdAt: string;
  updatedAt: string | null;
}

interface NextOfKinForm {
  fullName: string;
  relationship: string;
  phoneNumber: string;
  alternativePhoneNumber: string;
  email: string;
  physicalAddress: string;
}

@Component({
  selector: 'app-next-of-kin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './next-of-kin.component.html',
  styleUrl: './next-of-kin.component.scss'
})
export class NextOfKinComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api/employee-profile/me/next-of-kin';

  contacts: NextOfKinContact[] = [];

  isLoading = true;
  isSaving = false;
  showForm = false;
  editingId: number | null = null;

  message = '';
  errorMessage = '';

  form: NextOfKinForm = this.emptyForm();

  ngOnInit(): void {
    this.loadContacts();
  }

  private emptyForm(): NextOfKinForm {
    return {
      fullName: '',
      relationship: '',
      phoneNumber: '',
      alternativePhoneNumber: '',
      email: '',
      physicalAddress: ''
    };
  }

  loadContacts(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.http.get<NextOfKinContact[]>(this.apiUrl).subscribe({
      next: response => {
        this.contacts = response ?? [];

        if (this.contacts.length === 0) {
          this.beginAdd();
        } else if (this.editingId === null) {
          this.showForm = false;
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ||
          'Unable to load your Next of Kin contacts. Please try again.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  beginAdd(): void {
    this.editingId = null;
    this.form = this.emptyForm();
    this.showForm = true;
    this.message = '';
    this.errorMessage = '';
  }

  beginEdit(contact: NextOfKinContact): void {
    this.editingId = contact.id;
    this.form = {
      fullName: contact.fullName,
      relationship: contact.relationship,
      phoneNumber: contact.phoneNumber,
      alternativePhoneNumber: contact.alternativePhoneNumber ?? '',
      email: contact.email ?? '',
      physicalAddress: contact.physicalAddress ?? ''
    };
    this.showForm = true;
    this.message = '';
    this.errorMessage = '';
  }

  cancelForm(): void {
    if (this.contacts.length === 0) {
      return;
    }

    this.showForm = false;
    this.editingId = null;
    this.form = this.emptyForm();
    this.errorMessage = '';
  }

  saveContact(): void {
    if (this.isSaving) {
      return;
    }

    this.message = '';
    this.errorMessage = '';

    const payload = {
      fullName: this.form.fullName.trim(),
      relationship: this.form.relationship.trim(),
      phoneNumber: this.form.phoneNumber.trim(),
      alternativePhoneNumber: this.form.alternativePhoneNumber.trim() || null,
      email: this.form.email.trim() || null,
      physicalAddress: this.form.physicalAddress.trim() || null
    };

    if (!payload.fullName || !payload.relationship || !payload.phoneNumber) {
      this.errorMessage = 'Full name, relationship and phone number are required.';
      return;
    }

    this.isSaving = true;

    const request = this.editingId === null
      ? this.http.post<NextOfKinContact>(this.apiUrl, payload)
      : this.http.put<NextOfKinContact>(
          `${this.apiUrl}/${this.editingId}`,
          payload
        );

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;
        this.editingId = null;
        this.form = this.emptyForm();
        this.message = 'Next of Kin contact saved successfully.';
        this.loadContacts();
        this.cdr.detectChanges();
      },
      error: error => {
        this.isSaving = false;
        this.errorMessage =
          error.error?.message ||
          'Unable to save the contact. Please check the details and try again.';
        this.cdr.detectChanges();
      }
    });
  }

  deleteContact(contact: NextOfKinContact): void {
    if (this.contacts.length <= 1) {
      this.errorMessage = 'At least one Next of Kin contact must remain registered.';
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete ${contact.fullName} from your Next of Kin contacts?`
    );

    if (!confirmed) {
      return;
    }

    this.message = '';
    this.errorMessage = '';

    this.http.delete(`${this.apiUrl}/${contact.id}`).subscribe({
      next: () => {
        this.message = 'Next of Kin contact deleted successfully.';

        if (this.editingId === contact.id) {
          this.editingId = null;
          this.showForm = false;
          this.form = this.emptyForm();
        }

        this.loadContacts();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ||
          'Unable to delete the contact. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }
}
