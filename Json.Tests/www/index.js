// --------------------------------------- WebSocket ---------------------------------------
var connection;
var websocketCount = 0;
var req = 1;
var clt = null;
var requestStart;
var subSeq   = 0;
var subCount = 0;
var activeTab;

const responseState     = document.getElementById("response-state");
const subscriptionCount = document.getElementById("subscriptionCount");
const subscriptionSeq   = document.getElementById("subscriptionSeq");
const selectExample     = document.getElementById("example");
const socketStatus      = document.getElementById("socketStatus");
const reqIdElement      = document.getElementById("req");
const ackElement        = document.getElementById("ack");
const cltElement        = document.getElementById("clt");
const defaultUser       = document.getElementById("user");
const defaultToken      = document.getElementById("token");
const catalogExplorer   = document.getElementById("catalogExplorer");
const entityExplorer    = document.getElementById("entityExplorer");
const writeResult       = document.getElementById("writeResult");
const readEntitiesDB    = document.getElementById("readEntitiesDB");
const readEntities      = document.getElementById("readEntities");
const catalogSchema     = document.getElementById("catalogSchema");
const entityType        = document.getElementById("entityType");
const entityId          = document.getElementById("entityId");


class App {

    connectWebsocket () {
        if (connection) {
            connection.close();
            connection = null;
        }
        var loc     = window.location;
        var nr      = ("" + (++websocketCount)).padStart(3,0)
        var uri     = `ws://${loc.host}/ws-${nr}`;
        // var uri  = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';
        try {
            connection = new WebSocket(uri);
        } catch (err) {
            socketStatus.innerText = "connect failed: err";
            return;
        }
        connection.onopen = () => {
            socketStatus.innerHTML = "connected <small>🟢</small>";
            console.log('WebSocket connected');
            req         = 1;
            subCount    = 0;
        };

        connection.onclose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
            console.log('WebSocket closed');
        };

        // Log errors
        connection.onerror = (error) => {
            socketStatus.innerText = "error";
            console.log('WebSocket Error ' + error);
        };

        // Log messages from the server
        connection.onmessage = (e) => {
            var duration = new Date().getTime() - requestStart;
            var data = JSON.parse(e.data);
            // console.log('server:', e.data);
            switch (data.msg) {
            case "resp":
            case "error":
                clt = data.clt;
                cltElement.innerText  = clt ?? " - ";
                const content = this.formatJson(this.formatResponses, e.data);
                this.responseModel.setValue(content)
                responseState.innerHTML = `· ${duration} ms`;
                break;
            case "ev":
                subscriptionCount.innerText = ++subCount;
                subSeq = data.seq;
                // multiple clients can use the same WebSocket. Use the latest
                if (clt == data.clt) {
                    subscriptionSeq.innerText   = subSeq ? subSeq : " - ";
                    ackElement.innerText        = subSeq ? subSeq : " - ";
                }
                break;
            }
        };
    }

    closeWebsocket  () {
        connection.close();
    }

    getCookie  (name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
    }

    initUserToken  () {
        var user    = this.getCookie("fliox-user")   ?? "admin";
        var token   = this.getCookie("fliox-token")  ?? "admin";
        this.setUser(user);
        this.setToken(token);
    }

    setUser (user) {
        defaultUser.value   = user;
        document.cookie = `fliox-user=${user};`;
    }

    setToken  (token) {
        defaultToken.value  = token;
        document.cookie = `fliox-token=${token};`;
    }

    selectUser (element) {
        let value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    };

    addUserToken (jsonRequest) {
        var endBracket  = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        var before      = jsonRequest.substring(0, endBracket);
        var after       = jsonRequest.substring(endBracket);
        var userToken   = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken       = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    sendSyncRequest () {
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            this.responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
            responseState.innerHTML = "";
        } else {
            var jsonRequest = this.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                var request     = JSON.parse(jsonRequest);
                if (request) {
                    // Enable overrides of WebSocket specific members
                    if (request.req !== undefined) { req      = request.req; }
                    if (request.ack !== undefined) { subSeq   = request.ack; }
                    if (request.clt !== undefined) { clt      = request.clt; }
                    
                    // Add WebSocket specific members to request
                    request.req     = req;
                    request.ack     = subSeq;
                    if (clt) {
                        request.clt     = clt;
                    }
                }
                jsonRequest = JSON.stringify(request);                
            } catch { }
            responseState.innerHTML = '<span class="spinner"></span>';
            connection.send(jsonRequest);
            requestStart = new Date().getTime();
        }
        req++;
        reqIdElement.innerText  = req;
    }

    async postSyncRequest () {
        var jsonRequest         = this.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        let start = new Date().getTime();
        var duration;
        try {
            const response = await this.postRequest(jsonRequest, "POST");
            let content = await response.text;
            content = this.formatJson(this.formatResponses, content);
            duration = new Date().getTime() - start;
            this.responseModel.setValue(content);
        } catch(error) {
            duration = new Date().getTime() - start;
            this.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }

    lastCtrlKey;
    refLinkDecoration;

    applyCtrlKey(event) {
        if (this.lastCtrlKey == event.ctrlKey)
            return;
        this.lastCtrlKey = event.ctrlKey;
        if (!this.refLinkDecoration) {
            const cssRules = document.styleSheets[0].cssRules;
            for (let n = 0; n < cssRules.length; n++) {
                if (cssRules[n].selectorText == ".refLinkDecoration:hover")
                    this.refLinkDecoration = cssRules[n];
            }
        }
        this.refLinkDecoration.style.cursor = this.lastCtrlKey ? "pointer" : "";
    }

    onKeyUp (event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }

    onKeyDown (event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);

        switch (activeTab) {
        case "playground":
            if (event.code == 'Enter' && event.ctrlKey && event.altKey) {
                this.sendSyncRequest();
                event.preventDefault();
            }
            if (event.code == 'KeyP' && event.ctrlKey && event.altKey) {
                this.postSyncRequest();
                event.preventDefault();
            }
            if (event.code == 'KeyS' && event.ctrlKey) {
                // event.preventDefault(); // avoid accidentally opening "Save As" dialog
            }
            break;
        case "explorer":
            switch (event.code) {
                case 'KeyS':
                    if (event.ctrlKey)
                        this.execute(event, () => this.saveEntity());
                    break;
                case 'KeyP':
                    if (event.ctrlKey && event.altKey)
                        this.execute(event, () => this.sendCommand("POST"));
                    break;
                case 'ArrowLeft':
                    if (event.altKey)
                        this.execute(event, () => this.navigateEntity(this.entityHistoryPos - 1));
                    break;        
                case 'ArrowRight':
                    if (event.altKey)
                        this.execute(event, () => this.navigateEntity(this.entityHistoryPos + 1));
                    break;        
                }
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.ctrlKey}, alt:${event.altKey}`);
    }

    execute(event, lambda) {
        lambda();
        event.preventDefault();
    }

    // --------------------------------------- example requests ---------------------------------------
    async onExampleChange () {
        var exampleName = selectExample.value;
        if (exampleName == "") {
            this.requestModel.setValue("")
            return;
        }
        var response = await fetch(exampleName);
        var example = await response.text();
        this.requestModel.setValue(example)
    }

    async loadExampleRequestList () {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        var option = document.createElement("option");
        option.value    = "";
        option.disabled = true;
        option.selected = true;
        option.hidden   = true;
        option.text = "Select request ...";
        selectExample.add(option);

        var folder = './example-requests'
        var response = await fetch(folder);
        var exampleRequests = await response.json();
        var groupPrefix = "0";
        var groupCount  = 0;
        for (var example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            var name = example.substring(folder.length).replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = document.createElement("option");
            option.value    = example;
            option.text     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style    = groupCount % 2 ? "background-color: #ffffff;" : "background-color: #eeeeff;"
            selectExample.add(option);
        }
    }
    // --------------------------------------- Explorer ---------------------------------------
  

    async postRequest (request, tag) {
        let init = {        
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    request
        }
        try {
            const path          = `./?${tag}`;
            const rawResponse   = await fetch(path, init);
            const text          = await rawResponse.text();
            return {
                text: text,
                json: JSON.parse(text)
            };            
        } catch (error) {
            return {
                text: error.message,
                json: {
                    "msg":    "error",
                    "message": error.message
                }
            };
        }
    }

    async postRequestTasks (database, tasks, tag) {
        const db = database == "main_db" ? undefined : database;
        const request = JSON.stringify({
            "msg":      "sync",
            "database": db,
            "tasks":    tasks,
            "user":     defaultUser.value,
            "token":    defaultToken.value
        });
        tag = tag ? tag : "";
        return await this.postRequest(request, `${database}/${tag}`);
    }

    getRestPath(database, container, id, query) {
        let path = `./rest/${database}`;
        if (container)  path = `${path}/${container}`;
        if (id)         path = `${path}/${id}`;
        if (query)      path = `${path}?${query}`;
        return path;
    }

    async restRequest (method, body, database, container, id, query) {
        const path = this.getRestPath(database, container, id, query);        
        const init = {        
            method:  method,
            headers: { 'Content-Type': 'application/json' },
            body:    body
        }
        try {
            // authenticate with cookies: "fliox-user" & "fliox-token"
            return await fetch(path, init);
        } catch (error) {
            return {
                ok:     false,
                text:   () => error.message
            }
        }
    }

    getTaskError (content, taskIndex) {
        if (content.msg == "error") {
            return content.message;
        }
        var task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }

    errorAsHtml (message) {
        // first line: error type, second line: error message
        const pos = message.indexOf('>') + 1;
        const error = pos == 0 ? message : message.substring(0, pos) + "<br>" + message.substring(pos);
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }

    openTab (tabName) {
        activeTab = tabName;
        var tabContents = document.getElementsByClassName("tabContent");
        var tabs = document.getElementsByClassName("tab");
        for (var i = 0; i < tabContents.length; i++) {
            const tabContent = tabContents[i]
            tabContent.style.display = tabContent.id == tabName ? "block" : "none";
            if (tabContent.id == tabName) {
                tabs[i].classList.add("selected");
            } else {
                tabs[i].classList.remove("selected");
            }
        }
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }

    selectedCatalog;
    selectedEntity = {
        elem: null
    }

    setSelectedEntity(elem) {
        if (this.selectedEntity.elem) {
            this.selectedEntity.elem.classList.remove("selected");
        }
        this.selectedEntity.elem = elem;
        this.selectedEntity.elem.classList.add("selected");
    }

    async loadCluster () {
        const tasks = [
            { "task": "query", "container": "containers", "filterJson":{ "op": "true" }},
            { "task": "query", "container": "schemas",    "filterJson":{ "op": "true" }},
            { "task": "query", "container": "commands",   "filterJson":{ "op": "true" }}
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response = await this.postRequestTasks("cluster", tasks);
        const content = response.json;
        var error = this.getTaskError (content, 0);
        if (error) {
            catalogExplorer.innerHTML = this.errorAsHtml(error);
            return 
        }
        const dbContainers  = content.containers[0].entities;
        const dbSchemas       = content.containers[1].entities;
        const commands      = content.containers[2].entities;
        var ulCatalogs = document.createElement('ul');
        ulCatalogs.onclick = (ev) => {
            var path = ev.composedPath();
            const selectedElement = path[0];
            if (selectedElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            path[1].classList.add("active");
            if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
            this.selectedCatalog =selectedElement;
            selectedElement.classList.add("selected");
            const databaseName = selectedElement.childNodes[1].innerText;
            var dbCommands  = commands.find  (c => c.id == databaseName);
            var dbContainer = dbContainers.find  (c => c.id == databaseName);
            catalogSchema.innerHTML  = this.schemaLink(databaseName)
            this.listCommands(databaseName, dbCommands, dbContainer);
            // var style = path[1].childNodes[1].style;
            // style.display = style.display == "none" ? "" : "none";
        }
        let firstDatabase = true;
        for (var dbContainer of dbContainers) {
            var liCatalog       = document.createElement('li');
            if (firstDatabase) {
                firstDatabase = false;
                liCatalog.classList.add("active");
            }
            var liDatabase          = document.createElement('div');
            var catalogCaret        = document.createElement('div');
            catalogCaret.classList  = "caret";
            var catalogLabel        = document.createElement('span');
            catalogLabel.innerText  = dbContainer.id;
            liDatabase.title        = "database";
            catalogLabel.style = "pointer-events: none;"
            liDatabase.append(catalogCaret)
            liDatabase.append(catalogLabel)
            liCatalog.appendChild(liDatabase);
            ulCatalogs.append(liCatalog);

            var ulContainers = document.createElement('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                var path = ev.composedPath();
                const selectedElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (selectedElement.tagName.toLowerCase() != "div")
                    return;
                if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
                this.selectedCatalog = selectedElement;
                this.selectedCatalog.classList.add("selected");
                const containerName = this.selectedCatalog.innerText.trim();
                const databaseName  = path[3].childNodes[0].childNodes[1].innerText;
                var params = { database: databaseName, container: containerName };
                this.loadEntities(params);
            }
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                var liContainer     = document.createElement('li');
                liContainer.title   = "container";
                var containerLabel  = document.createElement('div');
                containerLabel.innerHTML = "&nbsp;" + containerName;
                liContainer.append(containerLabel)
                ulContainers.append(liContainer);
            }
        }
        this.createEntitySchemas(dbSchemas)
        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);
    }

    databaseSchemas = {};

    createEntitySchemas (dbSchemas) {
        var schemaMap = {};
        for (var dbSchema of dbSchemas) {
            var jsonSchemas     = dbSchema.jsonSchemas;
            var database        = dbSchema.id;
            const containerRefs = {};
            const rootSchema    = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            const containers    = rootSchema.properties;
            for (const containerName in containers) {
                const container = containers[containerName];
                containerRefs[container.additionalProperties.$ref] = containerName;
            }
            this.databaseSchemas[database] = dbSchema;
            dbSchema.containerSchemas = {}

            // add all schemas and their definitions to schemaMap and map them to an uri like:
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json#/definitions/PocStore
            for (var schemaPath in jsonSchemas) {
                var schema      = jsonSchemas[schemaPath];
                var uri         = "http://" + database + "/" + schemaPath;
                var schemaEntry = {
                    uri:            uri,
                    schema:         schema,
                    fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                    resolvedDef:    schema // not part of monaco > DiagnosticsOptions.schemas
                }
                schemaMap[uri] = schemaEntry;
                const definitions = schema.definitions;
                for (var definitionName in definitions) {
                    const definition = definitions[definitionName];
                    var path            = "/" + schemaPath + "#/definitions/" + definitionName;
                    var schemaId        = "." + path
                    var uri             = "http://" + database + path;
                    var containerName   = containerRefs[schemaId]
                    if (containerName) {
                        dbSchema.containerSchemas[containerName] = definition;
                    }
                    // add reference for definitionName pointing to definition in current schemaPath
                    var definitionEntry = {
                        uri:            uri,
                        schema:         { $ref: schemaId },
                        fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                        resolvedDef:    definition // not part of monaco > DiagnosticsOptions.schemas
                    }
                    schemaMap[uri] = definitionEntry;
                }
            }
            this.resolveRefs(jsonSchemas);
            this.addFileMatcher(database, dbSchema, schemaMap);
        }
        var monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);
    }

    resolveRefs(jsonSchemas) {
        for (const schemaPath in jsonSchemas) {
            // if (schemaPath == "Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Order.json") debugger;
            const schema      = jsonSchemas[schemaPath];
            this.resolveNodeRefs(jsonSchemas, schema, schema);
        }
    }

    resolveNodeRefs(jsonSchemas, schema, node) {
        const nodeType = typeof node;
        switch (nodeType) {
        case "array":
            console.log("array"); // todo remove
            return;
        case "object":
            const ref = node.$ref;
            if (ref) {
                if (ref[0] == "#") {
                    const localName = ref.substring("#/definitions/".length);
                    node.resolvedDef = schema.definitions[localName];
                } else {
                    const localNamePos = ref.indexOf ("#");
                    const schemaPath = ref.substring(2, localNamePos); // start after './'
                    const localName = ref.substring(localNamePos + "#/definitions/".length);
                    const globalSchema = jsonSchemas[schemaPath];
                    node.resolvedDef = globalSchema.definitions[localName];
                }
            }
            for (const propertyName in node) {
                if (propertyName == "resolvedDef")
                    continue;
                // if (propertyName == "employees") debugger;
                const property = node[propertyName];
                this.resolveNodeRefs(jsonSchemas, schema, property);
            }
            return;
        }
    }

    // add a "fileMatch" property to all container entity type schemas used for editor validation
    addFileMatcher(database, dbSchema, schemaMap) {
        var jsonSchemas     = dbSchema.jsonSchemas;
        var schemaName      = dbSchema.schemaName;
        var schemaPath      = dbSchema.schemaPath;
        var dbSchema        = jsonSchemas[schemaPath];
        var dbType          = dbSchema.definitions[schemaName];
        var containers      = dbType.properties;
        for (var containerName in containers) {
            const container     = containers[containerName];
            var containerType   = this.getResolvedType(container.additionalProperties, schemaPath);
            var uri = "http://" + database + containerType.$ref.substring(1);
            const schema = schemaMap[uri];
            var url = `entity://${database}.${containerName.toLocaleLowerCase()}.json`;
            schema.fileMatch.push(url); // requires a lower case string
        }
        var commandType     = dbSchema.definitions[schemaName + "Service"];
        var commands        = commandType.commands;
        for (var commandName in commands) {
            const command   = commands[commandName];
            // assign file matcher for command param
            var paramType   = this.getResolvedType(command.param, schemaPath);
            var url = `command-param://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (paramType.$ref) {
                var uri = "http://" + database + paramType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            } else {
                // uri is never referenced - create an arbitrary unique uri
                var uri = "http://" + database + "/command/param" + commandName;
                const schema = {
                    schema:     paramType,
                    fileMatch:  [url]
                };
                schemaMap[uri] = schema;
            }
            // assign file matcher for command result
            var resultType   = this.getResolvedType(command.result, schemaPath);
            var url = `command-result://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (resultType.$ref) {
                var uri = "http://" + database + resultType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            } else {
                // uri is never referenced - create an arbitrary unique uri
                var uri = "http://" + database + "/command/result" + commandName;
                const schema = {
                    schema:     resultType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
        }
    }

    getResolvedType (type, schemaPath) {
        var $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref };
    }

    getEntityType(schema, container) {
        if (!schema)
            return this.schemaLess;
        var dbSchema = schema.jsonSchemas[schema.schemaPath].definitions[schema.schemaName];
        var ref = dbSchema.properties[container].additionalProperties["$ref"];
        var lastSlashPos = ref.lastIndexOf('/');
        return ref.substring(lastSlashPos + 1);
    }

    setEditorHeader(show) {
        var displayEntity  = show == "entity" ? "" : "none";
        var displayCommand = show == "command" ? "" : "none";
        document.getElementById("entityTools")  .style.display = displayEntity;        
        document.getElementById("entityHeader") .style.display = displayEntity;        
        document.getElementById("commandTools") .style.display = displayCommand;
        document.getElementById("commandHeader").style.display = displayCommand;
    }

    getTypeLabel(type) {
        if (type.type) {
            return type.type;
        }
        if (type.$ref) {
            var lastSlash = type.$ref.lastIndexOf("/");
            return type.$ref.substring(lastSlash + 1);
        }        
        var result = JSON.stringify(type);
        return result = result == "{}" ? "any" : result;
    }

    schemaLess = '<span style="opacity:0.5">(schema less)</span>';

    getCommandTags(database, command, signature) {
        let label = this.schemaLess;
        if (signature) {
            const param   = this.getTypeLabel(signature.param);
            const result  = this.getTypeLabel(signature.result);
            label = `<span title="command parameter type"><span style="opacity: 0.5;">(param:</span> <span>${param}</span></span><span style="opacity: 0.5;">) : </span><span title="command result type">${result}</span>`
        }
        var link    = `command=${command}`;
        var url     = `./rest/${database}?command=${command}`;
        return {
            link:   `<a id="commandAnchor" title="command" onclick="app.sendCommand()" href="${url}" target="_blank" rel="noopener noreferrer">${link}</a>`,
            label:  label
        }
    }

    async sendCommand(method) {
        const value     = this.commandValueEditor.getValue();
        const database  = this.entityIdentity.database;
        const command   = this.entityIdentity.command;
        if (!method) {
            const commandAnchor =  document.getElementById("commandAnchor");
            let commandValue = value == "null" ? "" : `&value=${value}`;
            const path = this.getRestPath( database, null, null, `command=${command}${commandValue}`)
            commandAnchor.href = path;
            // window.open(path, '_blank');
            return;
        }
        const response = await this.restRequest(method, value, database, null, null, `command=${command}`);
        let content = await response.text();
        content = this.formatJson(this.formatResponses, content);
        this.entityEditor.setValue(content);
    }

    listCommands (database, dbCommands, dbContainer) {
        readEntitiesDB.innerHTML    = `<a title="database" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
        readEntities.innerHTML      = "";

        var ulDatabase  = document.createElement('ul');
        ulDatabase.classList = "database"
        var typeLabel = document.createElement('div');
        typeLabel.innerHTML = `<small style="opacity:0.5">type: ${dbContainer.databaseType}</small>`;
        ulDatabase.append(typeLabel)
        var commandLabel = document.createElement('div');
        commandLabel.innerHTML = `<a href="./rest/${database}?command=DbCommands" target="_blank" rel="noopener noreferrer"><small style="opacity:0.5" title="all database commands">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;commands</small></a>`;
        ulDatabase.append(commandLabel)

        var liCommands  = document.createElement('li');
        ulDatabase.appendChild(liCommands);

        var ulCommands = document.createElement('ul');
        ulCommands.onclick = (ev) => {
            this.setEditorHeader("command");
            var path = ev.composedPath();
            let selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent

            const tagName = selectedElement.tagName;
            if (tagName == "SPAN" || tagName == "DIV") {
                selectedElement = path[1];
            }
            const commandName = selectedElement.children[0].innerText;
            this.setSelectedEntity(selectedElement);

            this.showCommand(database, commandName);

            if (path[0].classList.contains("command")) {
                this.sendCommand("POST");
            }
        }
        for (const command of dbCommands.commands) {
            var liCommand = document.createElement('li');
            var commandLabel = document.createElement('div');
            commandLabel.innerText = command;
            liCommand.appendChild(commandLabel);
            var runCommand = document.createElement('div');
            runCommand.classList    = "command";
            runCommand.title        = "POST command"
            liCommand.appendChild(runCommand);

            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = ""
        liCommands.append(ulCommands);
        entityExplorer.appendChild(ulDatabase);
    }

    schemaLink(database) {
        const schema    = this.databaseSchemas[database];
        const name      = schema ? schema.schemaName : this.schemaLess;
        return `<a title="database schema" href="./rest/${database}?command=DbSchema" target="_blank" rel="noopener noreferrer">${name}</a>`;
    }

    filter = {
        database:   undefined,
        container:  undefined
    }

    runFilter(event) {
        if (event.code != 'Enter')
            return;
        var filter  = entityFilter.value;
        var query   = filter == "" ? null : `filter=${encodeURIComponent(filter)}`;
        var params  = { database: this.filter.database, container: this.filter.container };
        this.loadEntities(params, query);
    }

    updateFilterLink() {
        var filter  = entityFilter.value;
        var query   = filter == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        filterLink.href = url;
    }

    async loadEntities (p, query) {
        // if (p.clearSelection) this.setEditorHeader();
        this.filter.database     = p.database;
        this.filter.container    = p.container;
        
        // const tasks =  [{ "task": "query", "container": p.container, "filterJson":{ "op": "true" }}];

        catalogSchema.innerHTML  = this.schemaLink(p.database);
        readEntitiesDB.innerHTML = `<a title="database" href="./rest/${p.database}" target="_blank" rel="noopener noreferrer">${p.database}/</a>`;
        const containerLink      = `<a title="container" href="./rest/${p.database}/${p.container}" target="_blank" rel="noopener noreferrer">${p.container}/</a>`;
        readEntities.innerHTML   = `${containerLink}<span class="spinner"></span>`;
        const response           = await this.restRequest("GET", null, p.database, p.container, null, query);

        const reload = `<span class="reload" title='reload container' onclick='app.loadEntities(${JSON.stringify(p)})'></span>`
        writeResult.innerHTML   = "";        
        readEntities.innerHTML  = containerLink + reload;
        if (!response.ok) {
            const error = await response.text();
            entityExplorer.innerHTML = this.errorAsHtml(error);
            return;
        }
        let     content = await response.json();
        const   ids     = content.map(entity => entity.id);
        const   ulIds   = document.createElement('ul');
        ulIds.classList = "entities"
        ulIds.onclick = (ev) => {
            const path = ev.composedPath();
            const selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            if (selectedElement.tagName.toLowerCase() != "li")
                return;
            this.setSelectedEntity(selectedElement);
            const entityId = selectedElement.innerText;
            const params = { database: p.database, container: p.container, id: entityId };
            this.loadEntity(params);
        }
        for (var id of ids) {
            var liId = document.createElement('li');
            liId.innerText = id;
            ulIds.append(liId);
        }
        entityExplorer.innerText = ""
        entityExplorer.appendChild(ulIds);
    }

    entityIdentity = {
        database:   undefined,
        container:  undefined,
        entityId:   undefined,
        command:    undefined
    }

    entityHistoryPos    = -1;
    entityHistory       = [];

    storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection    = this.entityEditor.getSelection();
    }

    navigateEntity(pos) {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos = pos;
        const entry = this.entityHistory[pos]
        this.loadEntity(entry.route, true, entry.selection);
    }

    async loadEntity (p, preserveHistory, selection) {
        commandValueContainer.style.display = "none";
        commandParamBar.style.display = "none";
        this.layoutEditors();
        if (!preserveHistory) {
            this.storeCursor();
            this.entityHistory[++this.entityHistoryPos] = { route: {...p} };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
        this.entityIdentity = {
            database:   p.database,
            container:  p.container,
            entityId:   p.id
        };
        this.setEditorHeader("entity");
        const schema            = this.databaseSchemas[p.database];
        entityType.innerHTML    = this.getEntityType (schema, p.container);
        const containerRoute    = { database: p.database, container: p.container }
        let entityLink          = `<a href="#" style="opacity:0.7; margin-right:20px;" onclick='app.loadEntities(${JSON.stringify(containerRoute)})'>« ${p.container}</a>`;
        entityLink             += `<a title="entity id" href="./rest/${p.database}/${p.container}/${p.id}" target="_blank" rel="noopener noreferrer">${p.id}</a>`
        entityId.innerHTML      = `${entityLink}<span class="spinner"></span>`;
        writeResult.innerHTML   = "";
        const response  = await this.restRequest("GET", null, p.database, p.container, p.id);        
        let content   = await response.text();
        content = this.formatJson(this.formatEntities, content);
        const reload = `<span class="reload" title='reload entity' onclick='app.loadEntity(${JSON.stringify(p)}, true)'></span>`
        entityId.innerHTML = entityLink + reload;
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return;
        }
        // console.log(entityJson);
        this.setEntityValue(p.database, p.container, content);
        if (selection)  this.entityEditor.setSelection(selection);        
        this.entityEditor.focus();
    }

    async saveEntity () {
        const database  = this.entityIdentity.database;
        const container = this.entityIdentity.container;
        const jsonValue = this.entityModel.getValue();
        let id;
        try {
            var keyName = "id"; // could be different. keyName can be retrieved from schema
            id = JSON.parse(jsonValue)[keyName];
        } catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = 'save <span class="spinner"></span>';

        const response = await this.restRequest("PUT", jsonValue, database, container, id);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = "Save successful";
        // add as HTML element to entityExplorer if new
        if (this.entityIdentity.entityId != id) {
            this.entityIdentity.entityId = id;
            entityId.innerHTML = id;
            var liId = document.createElement('li');
            liId.innerText = id;
            liId.classList = "selected";
            const ulIds= entityExplorer.querySelector("ul");
            ulIds.append(liId);
            this.setSelectedEntity(liId);
            liId.scrollIntoView();
            this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, id:id }};
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
    }

    async deleteEntity () {
        const id        = this.entityIdentity.entityId;
        var container   = this.entityIdentity.container;
        var database    = this.entityIdentity.database;
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await this.restRequest("DELETE", null, database, container, id);
        if (!response.ok) {
            var error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Delete failed: ${error}</code>`;
        } else {
            writeResult.innerHTML = "Delete successful";
            entityId.innerHTML = "";
            this.setEntityValue(database, container, "");
            var selected = entityExplorer.querySelector(`li.selected`);
            selected.remove();
        }
    }

    entityModel;
    entityModels = {};

    getModel (url) {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            var entityUri   = monaco.Uri.parse(url);
            this.entityModel = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url] = this.entityModel;
        }
        return this.entityModel;
    }

    setEntityValue (database, container, value) {
        var url = `entity://${database}.${container}.json`;
        var model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel (model);
        if (value == "")
            return;
        var databaseSchema = this.databaseSchemas[database];
        if (!databaseSchema)
            return;
        try {
            const containerSchema   = databaseSchema.containerSchemas[container];
            this.decorateJson(this.entityEditor, value, containerSchema, database);
        } catch (error) {
            console.error("decorateJson", error);
        }
    }

    decorateJson(editor, value, containerSchema, database) {
        JSON.parse(value);  // early out on invalid JSON
        // 1.) [json-to-ast - npm] https://www.npmjs.com/package/json-to-ast
        // 2.) bundle.js created fom npm module 'json-to-ast' via:
        //     [node.js - How to use npm modules in browser? is possible to use them even in local (PC) ? - javascript - Stack Overflow] https://stackoverflow.com/questions/49562978/how-to-use-npm-modules-in-browser-is-possible-to-use-them-even-in-local-pc
        // 3.) browserify main.js | uglifyjs > bundle.js
        //     [javascript - How to get minified output with browserify? - Stack Overflow] https://stackoverflow.com/questions/15590702/how-to-get-minified-output-with-browserify
        const ast = parse(value, { loc: true });
        // console.log ("AST", ast);
        
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const oldDecorations = [];
        const newDecorations = [
            // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        this.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const start         = value.loc.start;
            const end           = value.loc.end;
            const range         = new monaco.Range(start.line, start.column, end.line, end.column);
            const markdownText  = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage  = [ { value: markdownText } ];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage }});
        });
        var decorations = editor.deltaDecorations(oldDecorations, newDecorations);
    }

    addRelationsFromAst(ast, schema, addRelation) {
        if (!ast.children) // ast is a 'Literal'
            return;
        for (const child of ast.children) {
            switch (child.type) {
            case "Object":
                this.addRelationsFromAst(child, schema, addRelation);
                break;
            case "Array":
                break;
            case "Property":
                // if (child.key.value == "employees") debugger;
                const property = schema.properties[child.key.value];
                if (!property)
                    continue;
                const value = child.value;

                switch (value.type) {
                case "Literal":
                    var relation = property.relation;
                    if (relation && value.value !== null) {
                        addRelation (value, relation);
                    }
                    break;
                case "Object":
                    var resolvedDef = property.resolvedDef;
                    if (resolvedDef) {
                        this.addRelationsFromAst(value, resolvedDef, addRelation);
                    }
                    break;
                case "Array":
                    var resolvedDef = property.items?.resolvedDef;
                    if (resolvedDef) {
                        this.addRelationsFromAst(value, resolvedDef, addRelation);
                    }
                    var relation = property.relation;
                    if (relation) {
                        for (const item of value.children) {
                            if (item.type == "Literal") {
                                addRelation(item, relation);
                            }
                        }
                    }
                    break;
                }
                break;
            }
        }
    }

    setCommandParam (database, command, value) {
        var url = `command-param://${database}.${command}.json`;
        var isNewModel = this.entityModels[url] == undefined;
        var model = this.getModel(url)
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel (model);
    }

    setCommandResult (database, command) {
        var url = `command-result://${database}.${command}.json`;
        var model = this.getModel(url)
        this.entityEditor.setModel (model);
    }

    showCommand(database, commandName) {
        commandValueContainer.style.display = "";
        commandParamBar.style.display = "";        
        this.layoutEditors();

        const schema        = this.databaseSchemas[database];
        const service       = schema ? schema.jsonSchemas[schema.schemaPath].definitions[schema.schemaName + "Service"] : null;
        const signature     = service ? service.commands[commandName] : null
        const def           = signature ? Object.keys(signature.param).length  == 0 ? "null" : "{}" : "null";
        const tags          = this.getCommandTags(database, commandName, signature);
        commandSignature.innerHTML      = tags.label;
        commandLink.innerHTML           = tags.link;

        this.entityIdentity.command     = commandName;
        this.entityIdentity.database    = database;
        this.setCommandParam (database, commandName, def);
        this.setCommandResult(database, commandName);
    }

    // --------------------------------------- monaco editor ---------------------------------------
    // [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults

    async createProtocolSchemas () {

        // configure the JSON language support with schemas and schema associations
        // var schemaUrlsResponse  = await fetch("/protocol/json-schema/directory");
        // var schemaUrls          = await schemaUrlsResponse.json();
        /* var schemas = [{
                uri: "http://myserver/foo-schema.json", // id of the first schema
                // fileMatch: [modelUri.toString()], // associate with our model
                schema: {
                    type: "object",
                    properties: {
                        p1: {
                            enum: ["v1", "v2"]
                        },
                        p2: {
                            $ref: "http://myserver/bar-schema.json" // reference the second schema
                        }
                    }
                }
            }, {
                uri: "http://myserver/bar-schema.json", // id of the second schema
                schema: {
                    type: "object",
                    properties: {
                        q1: {
                            enum: ["x1", "x2"]
                        }
                    }
                }
            }]; */
        var schemas = [];
        var jsonSchemaResponse  = await fetch("protocol/json-schema.json");
        var jsonSchema          = await jsonSchemaResponse.json();

        for (let schemaName in jsonSchema) {
            var schema          = jsonSchema[schemaName];
            var url             = "protocol/json-schema/" + schemaName;
            var schemaEntry = {
                uri:    "http://" + url,
                schema: schema            
            }
            schemas.push(schemaEntry);
        }
        return schemas;
    }

    requestModel;
    responseModel;

    requestEditor;
    responseEditor;
    entityEditor;
    commandValueEditor;

    requestContainer        = document.getElementById("requestContainer");
    responseContainer       = document.getElementById("responseContainer")
    commandValueContainer   = document.getElementById("commandValueContainer");
    commandParamBar         = document.getElementById("commandParamBar");
    commandValue            = document.getElementById("commandValue");
    entityContainer         = document.getElementById("entityContainer");

    allMonacoSchemas = [];

    addSchemas (monacoSchemas) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }

    async setupEditors ()
    {
        commandParamBar.style.display = "none";
        
        // --- setup JSON Schema for monaco
        var requestUri      = monaco.Uri.parse("request://jsonRequest.json");   // a made up unique URI for our model
        var responseUri     = monaco.Uri.parse("request://jsonResponse.json");  // a made up unique URI for our model
        var monacoSchemas   = await this.createProtocolSchemas();

        for (let i = 0; i < monacoSchemas.length; i++) {
            if (monacoSchemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolRequest.json") {
                monacoSchemas[i].fileMatch = [requestUri.toString()]; // associate with our model
            }
            if (monacoSchemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolMessage.json") {
                monacoSchemas[i].fileMatch = [responseUri.toString()]; // associate with our model
            }
        }
        this.addSchemas(monacoSchemas);

        // --- create request editor
        { 
            this.requestEditor = monaco.editor.create(requestContainer, { /* model: model */ });
            this.requestEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false },
                theme:          window.appConfig.monacoTheme,
            });
            this.requestModel = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel (this.requestModel);

            var defaultRequest = `{
    "msg": "sync",
    "tasks": [
        {
        "task":  "command",
        "name":  "Echo",
        "value": "Hello World"
        }
    ]
}`;
            this.requestModel.setValue(defaultRequest);
        }

        // --- create response editor
        {
            this.responseEditor = monaco.editor.create(responseContainer, { /* model: model */ });
            this.responseEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false }
            });
            this.responseModel = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel (this.responseModel);
        }

        // --- create entity editor
        {
            this.entityEditor = monaco.editor.create(entityContainer, { });
            this.entityEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false }
            });
            this.entityEditor.onMouseDown((e) => {
                if (!e.event.ctrlKey)
                    return;
                // console.log('mousedown - ', e);
                const value     = this.entityEditor.getValue();
                const column    = e.target.position.column;
                const line      = e.target.position.lineNumber;
                window.setTimeout(() => { this.tryFollowLink(value, column, line) }, 1);
            });
        }
        // --- create command value editor
        {
            this.commandValueEditor = monaco.editor.create(commandValue, { });
            // this.commandValueModel   = monaco.editor.createModel(null, "json");
            // this.commandValueEditor.setModel(this.commandValueModel);
            this.commandValueEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false }
            });
            //this.commandValueEditor.setValue("{}");
        }
        // this.commandResponseModel = monaco.editor.createModel(null, "json");

        window.onresize = () => {
            this.layoutEditors();        
        };
    }

    tryFollowLink(value, column, line) {
        try {
            JSON.parse(value);  // early out invalid JSON
            const ast               = parse(value, { loc: true });
            const database          = this.entityIdentity.database;
            const databaseSchema    = this.databaseSchemas[database];
            const containerSchema   = databaseSchema.containerSchemas[this.entityIdentity.container];

            let entity;
            this.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity)
                    return;
                const start = value.loc.start;
                const end   = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    entity = { database: database, container: container, id: value.value };
                }
            });
            if (entity) {
                this.loadEntity(entity);
            }
        } catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }

    formatEntities  = false;
    formatResponses = true;
    activeTab       = "playground";

    setConfig(key, value) {
        this[key] = value;
        const elem = document.getElementById(key);
        if (elem) {
            elem.value   = value;
            elem.checked = value;
        }
        window.localStorage.setItem(key, value);
    }

    getConfig(key) {
        return window.localStorage.getItem(key);
    }

    initConfigValue(key) {
        var valueStr = this.getConfig(key);
        if (valueStr == undefined) {
            this.setConfig(key, this[key]);
            return;
        }
        try {
            const value = JSON.parse(valueStr);
            this.setConfig(key, value);
        } catch (e) { }
    }

    loadConfig() {
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
    }

    formatJson(format, text) {
        if (format) {
            try {
                // const action = editor.getAction("editor.action.formatDocument");
                // action.run();
                const obj = JSON.parse(text);
                return JSON.stringify(obj, null, 4);
            }
            catch (error) {}            
        }
        return text;
    }

    layoutEditors () {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (activeTab) {
        case "playground":
            this.requestEditor?.layout();
            this.responseEditor?.layout();
            break;
        case "explorer":
            // layout from right to left. Otherwise commandValueEditor.clientWidth is 0px;
            this.layoutEditor(this.entityEditor,        entityContainer);
            this.layoutEditor(this.commandValueEditor,  commandValue);
            break;
        }
    }

    layoutEditor(monacoEditor, containerElem) {
        const editorElem = containerElem.children[0];
        if (!monacoEditor || !editorElem)
            return;
        editorElem.style.width = "0px";  // required to shrink width. Found no alternative solution right now.
        monacoEditor.layout();
        editorElem.style.width = containerElem.clientWidth + "px";
    }

    addTableResize () {
        var tdElm;
        var startOffset;
        const selector = document.querySelectorAll("table td, span div");

        Array.prototype.forEach.call(selector, (td) =>
        {
            if (!td.classList.contains("vbar"))
                return;
            td.style.position = 'relative';

            var grip = document.createElement('div');
            grip.innerHTML = "&nbsp;";
            grip.style.top = 0;
            grip.style.right = 0;
            grip.style.bottom = 0;
            grip.style.width = '7px'; // 
            grip.style.position = 'absolute';
            grip.style.cursor = 'col-resize';
            grip.style.userSelect = 'none'; // disable text selection while dragging
            grip.addEventListener('mousedown', (e) => {
                var previous = td.previousElementSibling;
                tdElm = previous;
                startOffset = previous.offsetWidth - e.pageX;
            });

            td.appendChild(grip);
        });

        document.addEventListener('mousemove', (e) => {
        if (tdElm && tdElm.style) {
            var width = startOffset + e.pageX + 'px'
            tdElm.style.width = width;
            var elem = tdElm.children[0];
            elem.style.width    = width;
            this.layoutEditors();
            // console.log("---", width)
        }
        });

        document.addEventListener('mouseup', () => {
            tdElm = undefined;
        });
    }
}

export const app = new App();
window.addEventListener("keydown", event => app.onKeyDown(event), true);
window.addEventListener("keyup", event => app.onKeyUp(event), true);
