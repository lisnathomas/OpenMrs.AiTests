# US-001: Clinician logs in to OpenMRS

Tags: @ui

As a clinician,
I want to log in with my username and password and choose my clinic location,
so that I can start seeing patients.

## Acceptance criteria

1. A user with valid credentials who chooses the "Outpatient Clinic" location lands on the OpenMRS home page.
2. After login, the top navigation shows the chosen location "Outpatient Clinic".
3. A wrong password keeps the user on the login page and shows "Invalid username or password".
4. An unknown username keeps the user on the login page and shows "Invalid username or password".

## Test data

Local demo admin account: username `admin`, password `Admin123`. Synthetic data only, never real patient data.
