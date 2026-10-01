# US-002: FHIR Patient API returns correct patient data

Tags: @api

As an integration partner (for example another hospital's system),
I want to read patient records from OpenMRS through its FHIR R4 API,
so that patient data can be exchanged accurately and securely between systems.

## Acceptance criteria

1. The capability statement (`/metadata`) is available without logging in and declares FHIR version "4.0.1".
2. An authenticated patient search returns a Bundle in which every patient has an id, a name and a gender.
3. Reading a patient by id returns the same patient as the search result (same id, family name, gender and birth date).
4. Searching by a patient's family name returns that patient.
5. Requesting a patient id that doesn't exist returns 404 with an OperationOutcome.
6. Requests without credentials are rejected with 401.
7. Requests with a wrong password are rejected with 401.

## Test data

Synthetic demo patients created by the OpenMRS reference application. Never use real patient data.
