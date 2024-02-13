# quip.net

Basic Quip API C#/.Net/Powershell wrapper.

## Powershell cmdlets

### Authentication

* To use this API, you need to define a user environment variable name __"QuipApiKey"__.
* Please go to https://quip.com/dev/token to generate your access token.

### Threads

* Get-QuipThread
Returns the given thread.

* Get-QuipThreadHtml
Returns the given thread HTML content.

* Get-QuipRecentThreads
Returns the most recent threads to have received messages, similar to the inbox view in the Quip app.

* New-QuipDocument
Creates a document or spreadsheet.

* Edit-QuipDocument
Incrementally modifies the content of a document.

### Messages

* Get-QuipThreadMessages
Returns a list of the most recent messages for the given thread, ordered reverse-chronologically.

* New-QuipThreadMessage
Adds a chat message to the given thread, posted as the authenticated user.

### Folders

* Get-QuipFolder
Returns the given folder.

### Users

* Get-QuipUser
Returns the given user.

* Get-QuipContacts
Returns a list of the contacts for the authenticated user.