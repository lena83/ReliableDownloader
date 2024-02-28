### .NET README

changes: 

- Created a library ReliableDownloader.Lib. I moved classes WebSystemCalls, FileDownloader, FileProgress to class library.
- I added exception handling as it was missing
- Added configuration file appsettings.json
- Added nlog for logging
- Added support of dependency injection in the main application
- used Polly library for timeouts
- added unit tests

Answers to questions: 

How did you approach solving the problem? I read readme file, tried to understand the problem. I read requirements. Then I started looking into the code and tried to implement a fast solution that would work. Then I started thinking how i would write the code in a way that I could potentially extend it, test it easier

How did you verify your solution works correctly? By running tests and testing in console app

How long did you spend on the exercise? maybe around 5 hours in total 

What would you add if you had more time and how? I would test timeouts more and in general would do more testing. Also added more unit tests. I would consider moving rewriting part where I'm getting reference file data.
