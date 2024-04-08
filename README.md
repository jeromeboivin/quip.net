# quip.net

Basic Quip API C#/.Net/Powershell wrapper.

## Powershell cmdlets

### Authentication

* To use this API, you need to define a user environment variable name __"QuipApiKey"__.
* Please go to https://quip.com/dev/token to generate your access token.

### Threads

#### Get-QuipThread
Returns basic information about a Quip thread (document, spreadsheet, or chat). This information includes things such as the ID, title, last time the thread was edited and the thread's link sharing settings.

```
SYNTAX
    Get-QuipThread [[-Id] <string>]

    Get-QuipThread [[-Url] <uri>]
```

```Powershell
> Get-QuipThread -Id OagQAILU5SVa

author_id    : fKcAEADae9L
thread_class : document
id           : abJAAAN5oSw
created_usec : 1699522528806636
updated_usec : 1710783549529972
title        : Sample document title
sharing      : libquip.threads.Sharing
link         : https://xxxx.quip.com/OagQAILU5SVa
type         : document
```

#### Get-QuipThreadHtml
Get the body of the thread in html format.

```
SYNTAX
    Get-QuipThreadHtml [[-Id] <string>]

    Get-QuipThreadHtml [[-Url] <uri>]
```

```Powershell
> Get-QuipThreadHtml -Id OagQAILU5SVa

<h1 id='temp:C:abJc03d17eb684d4ce79be3efb03'><span style="color:#e0218a" textcolor="#e0218a">Sample document title</span></h1>
```

#### Get-QuipRecentThreads
Returns the most recent threads to have received messages, similar to the updates view in the Quip app.

```
SYNTAX
    Get-QuipRecentThreads [-Count <int>]
```

```Powershell
> Get-QuipRecentThreads

Key         Value
---         -----
aAJAAAHz96F libquip.threads.Document
PcbAAAfVD8I libquip.threads.Document
YAeAAAgtfgJ libquip.threads.Document
fPfAAApiq3t libquip.threads.Document
XKZAAAls4hE libquip.threads.Document
CeWAAAP96VQ libquip.threads.Document
DdEAAAi16NX libquip.threads.Document
abJAAAN5oSw libquip.threads.Document
FVTAAAc6mSL libquip.threads.Document
UHYAAA9pqe9 libquip.threads.Document
```

#### New-QuipDocument
Creates a document or spreadsheet, returning the new thread in the same format as Get-QuipThread.

```
SYNTAX
    New-QuipDocument [-Title <string>] [-Content <string>] [-MemberIds <string>] [-Type {document | spreadsheet}]
    [-Format {html | markdown}]
```

```Powershell
> New-QuipDocument -Title "test" -Content "test content" -Type document -Format markdown

author_id    : VYAAEAAJdIN
thread_class : document
id           : OFYAAAVeoWV
created_usec : 1710784055625443
updated_usec : 1710784056246093
title        : test 2
sharing      :
link         : https://xxxx.quip.com/hrw1AneFl1cN
type         : document
```

#### Edit-QuipDocument
Modifies the content of a thread (Quip document or spreadsheet).

```
SYNTAX
    Edit-QuipDocument [[-Id] <string>] [-Thread <Thread>] [-SectionId <string>] [-Content <string>] [-Format {html |
    markdown}] [-Location {Append | Prepend | AfterSection | BeforeSection | ReplaceSection | DeleteSection}]
```

```Powershell
> Edit-QuipDocument -Id OFYAAAVeoWV -Content "added content" -Format markdown -Location Append

author_id    : VYAAEAAJdIN
thread_class : document
id           : OFYAAAVeoWV
created_usec : 1710784227433713
updated_usec : 1710784320592852
title        : test 2
sharing      :
link         : https://xxxx.quip.com/HOJYANDuHiJh
type         : document
```

### Messages

#### Get-QuipThreadMessages
Returns a list of the most recent messages for the given thread, ordered reverse-chronologically.

```
SYNTAX
    Get-QuipThreadMessages [[-ThreadId] <string>]

    Get-QuipThreadMessages [[-Thread] <Thread>]
```

```Powershell
> Get-QuipThreadMessages -ThreadId FdHAAAaIrTP

author_id        : VYAAEAAJdIN
id               : FdHADA6fRt6
created_usec     : 1710784418159659
text             : test message
annotation       :
author_name      : John Doe
mention_user_ids :
```

#### New-QuipThreadMessage
Adds a chat message to the given thread, posted as the authenticated user.

```
SYNTAX
    New-QuipThreadMessage [-ThreadId] <string> [-Content] <string> [-Frame] {bubble | card | line}

    New-QuipThreadMessage [-Thread] <Thread> [-Content] <string> [-Frame] {bubble | card | line}
```

```Powershell
> New-QuipThreadMessage -ThreadId FdHAAAaIrTP -Content "test answer 2" -Frame bubble

author_id        : VYAAEAAJdIN
id               : FdHADAbw4MP
created_usec     : 1710784560415675
text             : test answer 2
annotation       :
author_name      : John Doe
mention_user_ids :
```

### Folders

#### Get-QuipFolder
Returns the given folder.

```
SYNTAX
    Get-QuipFolder [-Id <string>]
```

```Powershell
> Get-QuipFolder -Id IpkaOO30ca7D

folder                 member_ids children
------                 ---------- --------
libquip.folders.Folder {}         {MSOAAAmi9JN, KIOAAADH9g7, WfeAAAGFpZM, VMEAAAwnuWv…}
```

### Users

#### Get-QuipUser
Returns the given user, or current connected user.

```
SYNTAX
    Get-QuipUser [-Id <string>]
```

```Powershell
> Get-QuipUser

name                : John Doe
id                  : VYAAEAAJdIN
affinity            : 0
desktop_folder_id   : XfSAOA4KgXn
archive_folder_id   : XfSAOAY1LdU
starred_folder_id   : XfSAOAUA9nB
private_folder_id   : XfSAOAUywdX
shared_folder_ids   : {AdJAOABd7DL, CbPAOA6caah, FIUAOARqibx, HcSAOAOgGVF…}
group_folder_ids    : {HIeAOA676yJ, AUUAOAwrdWo, EMSAOA1VWOv}
profile_picture_url : https://quip-cdn.com/umaG50GaExVfnn3ZrcPGXB
```

#### Get-QuipContacts
Returns a list of the contacts for the authenticated user.