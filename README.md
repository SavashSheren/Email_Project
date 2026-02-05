# Email_Project
📬 Messaging System with Admin Dashboard

A bootcamp project built with **ASP.NET Core MVC**, focusing on real-world backend logic, authentication, and data aggregation rather than simple CRUD operations.

This project includes a **full messaging system** and a **feature-rich admin dashboard** that provides meaningful insights into user activity.

---

🚀 Features

🔐 Authentication & Authorization
- User authentication with **ASP.NET Core Identity**
- Extended **AppUser** entity
- Secure login & registration flow

✉️ Messaging System
- Inbox & Sendbox structure
- **Read / Unread message logic**
- **Automatic message categorization** for incoming messages
- Category-based message management
- Soft delete (Trash) logic
- Clear Sender & Receiver separation

📊 Admin Dashboard
- KPI cards:
  - Monthly received messages
  - Monthly sent messages
  - Unread message count
  - Top contact (most frequent sender)
  - Top category (most used message category)
- **Daily / Weekly / Monthly message traffic analysis**
- Data visualization using **Chart.js**

---

🧠 Learning Focus

This project was developed during a bootcamp with a strong emphasis on:
- Implementing **unread message logic** correctly and consistently
- Designing **automatic message categorization logic**
- **Aggregating data** for dashboards (daily, weekly, monthly)
- Integrating **ASP.NET Core Identity** with custom business logic
- Writing clean and maintainable controller logic
- Avoiding tutorial-style or hardcoded solutions

---

🛠 Tech Stack
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server
- Razor Views
- Chart.js
- LINQ & asynchronous programming

---

🧩 Project Structure (Simplified)

/Controllers

MessageController

ProfileController

LoginController

RegisterController

/Entities

AppUser

Message

Category

/Views

Message

Profile

Admin Dashboard

/wwwroot
/screenshots

/Program.cs
/appsettings.json

---

📸 Screenshots

All screenshots are taken from the **running application** and stored under:


### Admin Dashboard
![Admin Dashboard](wwwroot/screenshots/admin-dashboard.png)

### Messaging Traffic
![Messaging Traffic](wwwroot/screenshots/messaging-traffic.png)

### Inbox / Unread Messages
![Inbox](wwwroot/screenshots/inbox.png)

### Profile Page
![Profile Page](wwwroot/screenshots/profile-page.png)

---

📌 Notes
- This project was built primarily for **learning purposes**
- Designed with a **real-world mindset**, not just academic requirements
- Open to refactoring, optimization, and improvements

---

🤝 Feedback
I’m open to feedback, suggestions, and code reviews.  
Always looking to improve and learn better practices.

---

🔖 Tags
ASP.NET Core · MVC · Entity Framework · Identity · C# · Backend Development
