### .NET README

changes: 

- Created a library ReliableDownloader.Lib. I moved classes WebSystemCalls, FileDownloader, FileProgress to class library.
- I added exception handling as it was missing
- Added configuration file appsettings.json
- Added nlog for logging
- Added support of dependency injection in the main application
- used Polly library for timeouts
- added unit tests
