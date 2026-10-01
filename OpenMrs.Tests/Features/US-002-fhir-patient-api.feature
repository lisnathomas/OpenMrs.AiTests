@US-002 @api
Feature: FHIR Patient API returns correct patient data
  As an integration partner (for example another hospital's system),
  I want to read patient records from OpenMRS through its FHIR R4 API,
  so that patient data can be exchanged accurately and securely between systems.

  Scenario: The capability statement is public and declares FHIR R4
    Given I am an anonymous FHIR API client
    When I request the FHIR capability statement
    Then the response status should be 200
    And the response should be a FHIR "CapabilityStatement" resource
    And the capability statement should declare FHIR version "4.0.1"

  Scenario: Patient search returns complete patient records
    Given I am an authenticated FHIR API client
    When I search for patients
    Then the response status should be 200
    And the response should be a FHIR "Bundle" resource
    And every patient in the bundle should have an id, a name and a gender

  Scenario: Reading a patient by id matches the search result
    Given I am an authenticated FHIR API client
    And I have searched for patients
    When I read the first patient from the search results by id
    Then the response status should be 200
    And the patient should match the first search result

  Scenario: Searching by family name finds the patient
    Given I am an authenticated FHIR API client
    And I have searched for patients
    When I search for patients by the family name of the first search result
    Then the response status should be 200
    And the first search result should be in the results

  Scenario: An unknown patient id returns 404
    Given I am an authenticated FHIR API client
    When I request the patient with id "00000000-0000-0000-0000-000000000000"
    Then the response status should be 404
    And the response should be a FHIR "OperationOutcome" resource

  Scenario: Requests without credentials are rejected
    Given I am an anonymous FHIR API client
    When I search for patients
    Then the response status should be 401

  Scenario: Requests with a wrong password are rejected
    Given I am a FHIR API client with username "admin" and password "wrong-password"
    When I search for patients
    Then the response status should be 401
