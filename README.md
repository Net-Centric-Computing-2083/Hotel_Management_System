# 🏨 Hotel Management System

A web-based **Hotel Management System** developed using **ASP.NET Core MVC** and **SQL Server**. The system helps hotel staff manage rooms, room types, customers, bookings, check-in/check-out operations, and billing through a centralized dashboard.

---

## 📌 Project Overview

The Hotel Management System is designed to simplify and organize common hotel operations.

The system provides hotel staff with a centralized platform to:

- Manage hotel rooms
- Manage room types
- Register and manage customers
- Create and manage bookings
- Check guests in and out
- Generate and manage bills
- Track room availability
- Monitor hotel activities through a dashboard

The application follows the **Model-View-Controller (MVC)** architecture provided by ASP.NET Core.

---

## ✨ Features

### 📊 Dashboard

The dashboard provides an overview of the hotel, including:

- Total rooms
- Available rooms
- Total bookings
- Total customers
- Quick access to major management modules

---

### 🛏️ Room Management

Staff can:

- Add new rooms
- View all rooms
- View room details
- Edit room information
- Delete rooms when permitted
- Track room availability
- View room type and capacity
- View price per night

Rooms cannot be deleted when they are associated with existing bookings.

---

### 🏷️ Room Type Management

The system supports different room types such as:

- Standard
- Deluxe
- Suite

Each room type contains:

- Room type name
- Capacity
- Associated rooms

Room types that are currently being used by rooms are protected from deletion.

---

### 👤 Customer Management

Staff can:

- Register customers
- View all customers
- View customer details
- Edit customer information
- Delete customers when permitted
- View customer booking information

Customer information includes:

- Full Name
- Phone
- Email
- Address

Customers with existing bookings cannot be deleted.

---

### 📅 Booking Management

The booking module allows staff to:

- Create new bookings
- View bookings
- View booking details
- Edit bookings
- Cancel bookings
- Delete bookings when permitted
- Select customers
- Select rooms
- Specify check-in and check-out dates
- Prevent overlapping bookings for the same room

The system validates booking dates to prevent a room from being booked by multiple customers during overlapping periods.

---

### 🛎️ Check-In / Check-Out

Hotel staff can manually manage guest arrival and departure.

#### Check-In

When a guest arrives:

1. Staff selects the booking.
2. The guest is checked in.
3. Check-in date and time are recorded.
4. The room is marked as unavailable.

#### Check-Out

When a guest leaves:

1. Staff selects the active booking.
2. The guest is checked out.
3. Check-out date and time are recorded.
4. The room becomes available again.

The system displays three stages:

- Confirmed
- Checked In
- Checked Out

---

### 💰 Billing

The billing module allows staff to:

- Generate bills for bookings
- Calculate room charges
- Add additional charges
- Calculate total amount
- Mark bills as paid
- View bill details
- Delete unpaid bills

Room charges are calculated based on:

**Number of nights × Room price per night**

---

### 🔎 Room Availability

The system can check room availability based on booking dates.

A room is considered unavailable for a requested period when an existing booking overlaps with the requested check-in and check-out dates.

The overlap condition used is:

```text
ExistingCheckIn < RequestedCheckOut
AND
ExistingCheckOut > RequestedCheckIn
