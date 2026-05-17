# Clinic Management System

A comprehensive role-based Clinic Management System developed using C# (.NET Framework), Windows Forms, ADO.NET, and SQL Server. The system automates clinic workflows including patient management, appointments, queue handling, consultations, billing, prescriptions, pharmacy inventory, and reporting. :contentReference[oaicite:0]{index=0}

---

## Overview

Healthcare clinics often struggle with manual record keeping, disconnected systems, appointment confusion, billing issues, and poor coordination between departments. This project provides a centralized digital solution that integrates all major clinic operations into a single platform for improved efficiency, accuracy, and patient care. :contentReference[oaicite:1]{index=1}

---

## Features

### Authentication & Security
- Role-based login system
- Password hashing
- Profile and credential management

### Patient & Appointment Management
- Patient registration
- Scheduled appointments
- Walk-in patient handling
- Queue management
- Appointment calendar

### Doctor Module
- Real-time patient queue
- Consultation management
- Prescription generation
- Patient history tracking

### Pharmacist Module
- Prescription dispensing
- Inventory management
- Stock tracking
- Sales history

### Billing & Reports
- Billing management
- Payment tracking
- Printable receipts
- Prescription history
- Inventory reports
- Audit reports

---

## User Roles

### Admin
- Manage staff
- Manage users
- Manage doctors
- View reports
- Monitor clinic operations

### Receptionist
- Register patients
- Schedule appointments
- Manage queue
- Handle billing
- Check-in patients

### Doctor
- View appointments
- Manage consultations
- Create prescriptions
- Access patient history

### Pharmacist
- Dispense medicines
- Manage inventory
- Track sales
- Monitor stock levels

---

## Technologies Used

### Programming Language
- C# (.NET Framework)

### User Interface
- Windows Forms (WinForms)
- GDI+ UI Styling
- DataGridView

### Database
- Microsoft SQL Server
- ADO.NET

### Development Tools
- Visual Studio
- SQL Server Management Studio (SSMS)

---

## Database Design

The system uses a relational SQL Server database with normalized tables to ensure data integrity and reduce redundancy.

### Main Tables
- Users
- Patients
- Doctors
- Appointments
- Appointment Audit Logs
- Visits
- Queues
- Prescriptions
- Prescription Details
- Medicines
- Sales
- Bills
- Bill Items
- Staff

### Key Relationships
- One patient can have multiple appointments
- Walk-in patients are directly added to queue
- Each appointment generates one prescription
- Prescriptions reference medicines
- Billing records link with appointments
- Payment status updates in real time

---

## System Workflow

1. Receptionist registers patient
2. Appointment or walk-in token is created
3. Patient enters doctor queue
4. Doctor performs consultation
5. Prescription is generated
6. Pharmacist dispenses medicines
7. Receptionist completes billing
8. Payment status updates to PAID

---

## Screens / Modules

### Admin Dashboard
- Clinic overview
- Staff management
- Reports
- Settings

### Receptionist Dashboard
- Queue handling
- Patient registration
- Billing workflow

### Doctor Dashboard
- Daily appointments
- Queue overview
- Patient consultation

### Pharmacist Dashboard
- Prescription queue
- Inventory alerts
- Medicine dispensing

---

## Dynamic UI Concept

Each form loads dynamically inside the main dashboard without opening multiple windows, improving workflow efficiency and user experience.

---

## Testing Performed

- Login and role testing
- Queue synchronization testing
- Billing workflow testing
- Prescription testing
- Inventory stock testing
- Walk-in patient testing

All modules functioned successfully during testing.

---

## Future Enhancements

- Online appointment booking
- SMS notifications
- Online payment gateway
- Mobile application
- Advanced analytics
- Multi-branch support

---

## Installation

1. Clone the repository

```bash
git clone https://github.com/salehakhuram/Clinic-Management-System-.git
