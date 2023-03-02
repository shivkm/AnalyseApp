Feature: Calculate team strength with Poisson distribution

Scenario: Calculate the  Strength of Crystal Palace and Leicester 
	Given 20 played matched in league
	And ten matches played by crystal palace at home side
	When the poisson distribution calculate the strength
	Then the result should be 120