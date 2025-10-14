# Movie Tickets – Assignment 2


# Repo notes:

## Run:
- `git clone git@github....` 
- `export TMDB__APIKEY="df0b8bc6934d37266ef32754dfa21420"`
- `curl -X POST http://localhost:5000/api/admin/import/tmdb`
- `dotnet run --project tools/SeedGenerator`
- `./start.sh`

> [!NOTE]
> To refresh the movie catalog from TMDB, set `TMDB__APIKEY` (or edit `backend/src/MovieTickets.Api/appsettings.json`) and call `POST http://localhost:5000/api/admin/import/tmdb`... and rerun: `dotnet run --project tools/SeedGenerator`.

File structure is relavtively self-explanatory. 

- `/backend`: Stores logic and seeded data. 
- `/desktop`: Electron, nuff said.
- `/frontend`: Honestly, I have no idea, shit is straight up vibe-coded, I don't do UI, I know enough to theme my dotfiles but making a website? couldn't care less.
- `/storage`: Theoretically, this directory isn't needed, but I'm not bothered enough to refactor it. Tldr, its just filler data. We would curl first, then use SeedGenerator, as the CopyToOutputDirectory is set as PreserveNewest, out data won't overrite itself.

> [!WARNING] 
> After navigating towards the seat selection screen, the 'Quick Book' option is there for demo/testing, it auto-fills your current selection with adults and calls the same booking endpoint. It is not indicative of actual performance (nor is the rest of data)

## For Windows:

WSL with Ubuntu: (doesnt matter, nix is distro-agnostic)
- `sudo apt update && sudo apt upgrade -y`
- `sh <(curl -L https://nixos.org/nix/install) --no-daemon . ~/.nix-profile/etc/profile.d/nix.sh` # installs nix

- `sudo usermod -aG nix-users $USER` # this should be the main one
- `sudo usermod -aG nixbld $USER` # but I also have this just in case...
# adds ur user group, make sure to run `wsl --shutdown` in Powershell (admin) and rerun with `wsl.exe -d Ubuntu-24.04` or whatever you use. 

- `mkdir -p ~/.config/nix`
- `echo "experimental-features = nix-command flakes" >> ~/.config/nix/nix.conf`
- `nix --version` # verify
- `sudo systemctl start nix-daemon` # just in case
- `sudo ls -ld /nix/var/nix /nix/var/nix/daemon-socket /nix/var/nix/daemon-socket/socket` # final check, for if you have perms to run nix commands or not
- `nix develop` # should install stuff

- `which dotnet` # result should be /nix/store/.../dotnet-sdk-8.0.300/bin/dotnet

- `sed -i 's/\r$//' start.sh`
- `chmod +x start.sh`

- `export TMDB__APIKEY="df0b8bc6934d37266ef32754dfa21420"`
- `curl -X POST http://localhost:5000/api/admin/import/tmdb`
- `dotnet run --project tools/SeedGenerator`
- `./start.sh`

---

## Assignment brief

**31927 - Applications Development with .NET**  
**32998 - .NET Applications Development**

**SPRING 2025**

## ASSIGNMENT-2 SPECIFICATION

**Due date:** Friday 11:30pm, 17 October 2025  
**Demonstrations:** Required in the lab/tutorial session  
**Marks:** 35% of the total marks for this subject  
**Submission:** Complete project folder zip (Code, solution files, Project Description, etc.), report, any instructions to run the program in a text file, all in 1 single zip file. Submit to Canvas assignment submission  

**Note:** This assignment is group work and individually assessed.

## Summary

This assessment requires you to develop an application with the necessary graphical user interface - GUI (e.g., Windows Form/MAUI) and underlying functionality based on your proposed topic. This is a group assignment, and each group should ideally consist of a minimum of two and a maximum of three students. All group must be from same lab.

## Assignment Objectives

The purpose of this assignment is to demonstrate competence in the following skills.

- GUI/Windows form and controls
- Communication between multiple interfaces
- Using collections/generics/delegates
- Enumerators, properties, extension methods.
- File/database reading and writing and Entity Framework
- Test cases/NUnit

**NOTE:** Before proceeding, ensure you obtain pre-approval for your chosen topic from your tutor or subject coordinator, followed by project information submission (https://forms.office.com/r/deQr7rTGXt). It is crucial to seek their confirmation to ensure alignment with the course objectives and guidelines.

## Marking Guide

Below is the marking guide for this assessment. It is designed to allow you to get a Pass grade with minimal effort while still demonstrating that you understand the core principles of .NET development, to get a Distinction with reasonable effort, and to get a High Distinction with solid effort, and 100% with considerable effort. It is recommended that you pay attention to the grade distribution and work towards your own skill level with your team members.

In the demos in the lab, your code needs to be compiled in Visual Studio 2022 (with .NET 7.0 or higher) and then the tutor will test for normal functionality as described in the descriptions above. If your code does not compile, you will receive zero marks. You need to demonstrate that you understand the functionality of various components presented by you and should be easily readable and usable by your tutor.

| Task Items | Max Points |
|------------|------------|
| **Project report** • Project registration (get pre-approval from coordinator/tutor before submission) - https://forms.office.com/r/deQr7rTGXt • Project idea description, motivation, key features and contribution of each team member with references as needed (1500-2000 words) | 3 |
| **Project idea** Unique and technically challenging project addressing real-world problem | 3 |
| **Code Quality** • Includes proper indenting and white spacing. • Helpful comments • Meaningful class/method/property/field names. | 3 |
| **Code Requirement** • Includes high cohesion and low coupling for classes and methods • At least one example of polymorphism which achieves a useful purpose (either through inheritance, method/constructor overloading/overriding) • At least two examples of Interface • At least one example of NUnit tests • At least one example of Anonymous method with LINQ using Lambda expression • At least one example of Generics/Generic based Collection | 6 |
| **Interface Design** • At least four GUI forms/screens/interfaces which are resizable & responsive that have their own distinct feature or functionality. e.g. pointless screens like a welcome screen don't count. • At least six different unique categories of UI elements have been used. e.g. buttons, headings, dropdowns, images, bar graphs, carousels, lists, context menus, modals etc. | 10 |
| **Functionality** • Successful implementation of core features and requirements as per project description. • Adequate error handling • Input validations • Use of appropriate data structures and algorithms. | 10 |
| **Total (without bonus marks)** | **35** |

### Bonus Marks
- Use of either Blazor, ASP.NET, WPF or some other UI library instead of Windows Forms: **2 points**
- Use of external database with LINQ • Use of Entity Framework • Use of external APIs or tools including data analytics or machine learning: **3 points**

## Assignment Submission

1. Make sure to submit project registration information using https://forms.office.com/r/deQr7rTGXt before the project submission.
2. Assignment should be submitted only by team leader as specified in the project registration form.
3. You must upload a zip file of the C# solution to Canvas with a maximum 1500-2000 words long PDF file explaining the project idea, motivation, key features, usage instructions and contribution of each team member with references as needed.
4. You may submit as many times as you like until the due date. The final submission you make is the one that will be marked. If you have not uploaded your zip file within 7 days of the Due Date, or it cannot be compiled and run in the lab, then your assignment will receive a zero mark.

**NOTE 1:** It is your group's responsibility to make sure you have thoroughly tested your program to make sure it is working correctly.

**NOTE 2:** Your final submission to Canvas is the one that is marked. It does not matter if earlier submissions were working; they will be ignored. Download your submission from Canvas and test it thoroughly in your assigned laboratory.

## Queries

If you have a problem such as illness affecting your assignment submission, contact the subject coordinator as soon as possible.

**Dr. Avinash Singh**  
Room: CB11.07.115  
Phone: 9514 1825  
Email: avinash.singh@uts.edu.au

If you have a question about the assignment, please post it to the Canvas discussion board for this subject so that everyone can see the response.

If serious problems are discovered in assignment specification, the class will be informed via an announcement on Canvas/EdSteam. It is your responsibility to make sure you frequently check Canvas.

**PLEASE NOTE:** If the answer to your questions can be found directly in any of the following:
- Subject outline
- Assignment specification
- Canvas FAQ and addendum
- Canvas discussion board

You will be directed to these locations rather than given a direct answer.

## Extensions and Special Consideration

Please refer to subject outline.

## Academic Standards and Late Penalties

Please refer to subject outline.
