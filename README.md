# EvaluaME Academic Platform

A cloud-based academic platform built with ASP.NET 8 and AdminLTE v3, integrated with Aiven MySQL, Jobe Server (OCI), and EscolaLMS/H5P APIs.
The system provides role-based access, exam management, coding evaluation, and cloud database architecture for modern educational workflows.

---

## 🎯 Purpose

1. **Provide** a cloud-ready academic platform for educators and students.
2. **Manage** exams, quizzes, and interactive learning content efficiently.
3. **Enable** automated code evaluation and online assessments.
4. **Integrate** with cloud databases and APIs for scalable performance.
5. **Offer** role-based access for administrators, instructors, and students.

---

## ✨ Key Features

- Role-Based Access: Different views and permissions for students, teachers, and admins.
- Exam & Quiz Management: Create, schedule, and monitor assessments.
- Coding Evaluation: Integrated with Jobe Server for automatic code execution and grading.
- Interactive Content: H5P support via EscolaLMS APIs for multimedia-rich learning activities.
- Cloud Database: Aiven MySQL backend for scalable and secure data storage.
- Admin Dashboard: Built with AdminLTE v3 for intuitive navigation and analytics.
- RESTful APIs: Integration-ready endpoints for external tools and services.

---

## 🛠️ Stack

| Layer	          | Technology                         |
|-----------------|------------------------------------|
| Backend         |	ASP.NET 8                          |
| Frontend        |	AdminLTE v3, HTML, CSS, JavaScript |
| Database        |	Aiven MySQL (cloud)                |
| Code Evaluation |	Jobe Server (OCI)                  |
| Interactive     | LMS	EscolaLMS + H5P APIs           |
| Deployment      |	Cloud-ready architecture           |

---

## ⚙️ Local Installation (Developers)

```bash
# 1. Clone repository
$ git clone https://github.com/Cheleizaguirre/EvaluaME-Academic-Platform.git
$ cd EvaluaME-Academic-Platform

# 2. Restore .NET dependencies
$ dotnet restore

# 3. Configure database connection
# Edit appsettings.json with Aiven MySQL credentials

# 4. Run the application
$ dotnet run
```

> **Note:** The app will run locally, typically at https://localhost:5001/. Ensure Jobe Server and API integrations are configured for full functionality.

---

## 🧠 How It Works

1. Users log in with role-based authentication.
2. Admins and instructors can create exams, quizzes, and H5P content.
3. Students access assessments and submit code or answers online.
4. Submitted code is executed and graded via the Jobe Server integration.
5. Data is stored and managed in the cloud using Aiven MySQL.
6. The frontend dashboard (AdminLTE) provides analytics, reports, and management tools.
7. APIs enable integration with EscolaLMS and other educational tools.

---

## 🚀 Future Improvements

- Add notifications and real-time exam monitoring.
- Enhance analytics dashboards with charts and trends.
- Implement multi-language support for international students.
- Expand API endpoints for LMS interoperability.
- Deploy full cloud CI/CD pipeline for scalability.

---

## 🤝 Contributing

1. Fork the repository and create a new branch (git checkout -b feature/YourFeature).
2. Commit changes with clear and descriptive messages.
3. Open a Pull Request describing your improvements or fixes.

---
