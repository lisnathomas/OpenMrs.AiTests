@US-001 @ui
Feature: Clinician logs in to OpenMRS
  As a clinician,
  I want to log in with my username and password and choose my clinic location,
  so that I can start seeing patients.

  Background:
    Given I am on the OpenMRS login page

  Scenario: A clinician logs in and chooses their clinic location
    When I log in as the admin user
    And I choose the "Outpatient Clinic" login location
    Then I should see the OpenMRS home page
    And the top navigation should show the location "Outpatient Clinic"

  Scenario: A wrong password is rejected
    When I log in with username "admin" and password "wrong-password"
    Then I should still be on the login page
    And I should see the login error "Invalid username or password"

  Scenario: An unknown username is rejected
    When I log in with username "no-such-user" and password "Admin123"
    Then I should still be on the login page
    And I should see the login error "Invalid username or password"
