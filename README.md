## **Bachelor thesis** https://dspace.cvut.cz/entities/publication/e07c3017-aaba-4529-a8ce-10d72e4db3fc
 - system for administration the delegations for the Prague Football Association [PFS
 ](https://www.fotbalpraha.cz/)

## Description
> PFS delegates **+-350** matches a week throughout the whole Prague, it is difficult to delegate and coordinate all **250** possible active referees (absences, locations, level,vetoes)

- Current situation

  The people whose delegate are using various excel tables , whose are: 

  1. **List of referees and their levels**. It determines which referee is at which level, which competition he referees primarily.  
  [Nomination_paper](Materials_used_by_pfs/Nomination_paper.xlsx) -_partially formatted_- 
  2. **Table of reported excuses on given game days/part of the days** .     Individual referees send every week eventual excuses to the association mail, those excuses from mails are manually overwritten to this file.In this file are also included informations about residence of referees and veto from clubs.  
  [Excuses](Materials_used_by_pfs/Excuses.xlsx) -_totally unformatted_
  3. **List of matches for future week**. This file is every week downloaded from registrace.fotbal.cz site, the privileged access must be assigned.  
  [List_matches](Materials_used_by_pfs/List_matches.xls) -_totally formatted_ 
  4. **Document r/team home/team away**. Where is recorded how many times the person has refereed to the given team at home and away.  
  _the state unknown_

- Procces of delegation

  it is done for a upcoming week 2 weeks in advance,the whole process of putting together a complete delegation can be divided into phases :  

  1. **filling up the list** of which referee has whose teams the previous weekend, it is preparation before each weekend,  - manually
  2. **collecting and rewriting excuses** from the association's email into an Excel document - manually 
  3. **combining matches** that take place immediately after each other on the same/closely located fields - manually
  4. **delegation itself**  
  the matches are extracted from the List_matches.xls, the matches are delegated with priority from the highest and are assigned by individual referees in the Excuses.xlsx, where apologies and vetoes from the clubs are already taken into account, we also have to take into account how many times the referee was on the team from the table and special requirements.




- Planned features and status - 

  - [x] **automatic filling the database table , where is the information about which referee is on which competition level**   
  _excel parsing_
  - [x] **automatic filling the database table, where are the records of how many times was referee delegated on the specific team (we distinguish home and away matches)**     
  _by api or document parsing_
  - [x] **connecting similar matches, these whose follow-up
  some other match**   
  _algorithmically, we can adjust the criteria when they are similar_
  - [x]  **loading excuses,vehicle availability,place of embarkation for the game day, vetoes for the next weekend**   
  _excel document parsing, in the case of implementation by a web app it can be done without the need for mail, directly referees enter the data to database via the web app_ 
  - [x] **dividing referees by level, displaying all the informations together , visually , it means displaying when the referee is time free throughout game days , so he is not excused or he does not have a match in the moment, displaying actuall informations about the amount of matches refereed for the team , which teams he cannot referee , because of the team veto, or personal reasons**  
_main part ,all visual, make simpler for the administrators whose delegate, can be divided into smaller steps_
  - [x] **planning the route between the matches , both possiblities , by car and public transport**  
_by idos.cz api and mapy.cz api whose are free to use_
  - [x] **personal card of referee , displaying schedule of the weekend(matches, transports) and all the informations for the specific person**  
  _frontend component_
  - [x] **automatic generation of possible delegations based on the point system**    
  _algorithmically, we can adjust the points criteria, in order to their importance_

  So in summary this system should make this work        easier and more transparent for them.


## Usage
> now the persons responsible for the administration and scheduling of matches spend a total of up to **50** hours a week.

The purpose of use is deployment in real proccess of administration, potentially, if possible, expansion among other football organizations.



## Roadmap
- [x] come up with a theme
- [x] finding a supervisor on the Czech side - Mr Ing. Miroslav Balík, Ph.D.
- [x] analysis of possible technological solutions
- [x] finding a supervisor on the Slovenian side - Mr asist. dr. Sandi Gec
- [x] some basic milestones
- [x] [Database structure
 ](https://lucid.app/lucidchart/26de1232-7aa1-4b02-b15e-1abc889a57ff/edit?invitationId=inv_24539d79-aa84-4e0f-86af-0fca5d8d9971)
- [x] working login/referee side
- [x] code on gitlab

## Project status
_testing_
