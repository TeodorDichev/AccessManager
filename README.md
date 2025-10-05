# AccessManager

**AccessManager** is an ASP.NET Core (.NET 9) web application for managing users, their organizational structure, and access levels to information systems. It allows administrators to control user permissions across departments, units, and systems, with full audit tracking and soft-deletion support.

---

## 🚀 Features

- **Entity Management**
  - Create, edit, and soft-delete all entities.
  - Assign users to departments and units.
  - Manage read/write permissions per information system.
  - Hierarchical access assignment: system → access → directive.
  - All significant actions (create, edit, delete, access assignment) are recorded in the `Log` table.

- **Frontend**
  - Clean Razor views with full Bulgarian localization.
  - External JavaScript for UI logic (no inline scripts or `<form>` submissions).
  - AJAX used for deactivation, access updates, and dynamic dropdowns.

---

## 🛠️ Tech Stack

- **Backend:** ASP.NET Core 9 (C#)
- **Frontend:** Razor Pages, JavaScript, jQuery, AJAX
- **Database:** Entity Framework Core with SQL Server
- **Language:** Bulgarian (UI)
- **Version Control:** Git

---

## ⚙️ Setup

- The application has been successfully deployed and integrated in a real local business environment.
- You can try the demo by cloning the repository, publishing it as a self-contained deployment, and hosting it on your local network.
- The system has been tested with up to **1,000 real users** and **10,000 information systems**.

---

## 📄 License

This project is licensed under the MIT License.
