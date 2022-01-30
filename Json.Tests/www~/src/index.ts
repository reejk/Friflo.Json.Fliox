/// <reference types="../../../node_modules/monaco-editor/monaco" />
/// <reference types="../../../node_modules/@types/json-to-ast/index" />

import { el, createEl, Resource, Method, ConfigKey, Config, defaultConfig } from "./types.js"
import { Schema, MonacoSchema } from "./schema.js"
import { Explorer }     from "./explorer.js";
import { EntityEditor } from "./entity-editor.js";

import { FieldType, JsonType }                                  from "../../assets~/Schema/Typescript/JsonSchema/Friflo.Json.Fliox.Schema.JSON";
import { DbSchema, DbContainers, DbCommands, DbHubInfo }        from "../../assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";
import { SyncRequest, SyncResponse, ProtocolResponse_Union }    from "../../assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol";
import { SyncRequestTask_Union, SendCommandResult }             from "../../assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.Tasks";


// declare const parse : any; // https://www.npmjs.com/package/json-to-ast
declare function parse(json: string, settings?: jsonToAst.Options): jsonToAst.ValueNode;

declare global {
    interface Window {
        appConfig: { monacoTheme: string };
        setTheme(mode: string) : void;
        app: App;
    }
}


// --------------------------------------- WebSocket ---------------------------------------
let connection:         WebSocket;
let websocketCount      = 0;
let req                 = 1;
let clt: string | null  = null;
let requestStart: number;
let subSeq              = 0;
let subCount            = 0;


const hubInfoEl         = el("hubInfo");
const responseState     = el("response-state");
const subscriptionCount = el("subscriptionCount");
const subscriptionSeq   = el("subscriptionSeq");
const selectExample     = el("example")         as HTMLSelectElement;
const socketStatus      = el("socketStatus");
const reqIdElement      = el("req");
const ackElement        = el("ack");
const cltElement        = el("clt");
const defaultUser       = el("user")            as HTMLInputElement;
const defaultToken      = el("token")           as HTMLInputElement;
const catalogExplorer   = el("catalogExplorer");
const entityExplorer    = el("entityExplorer");
const writeResult       = el("writeResult");

const entityFilter      = el("entityFilter")    as HTMLInputElement;

// request response editor
const requestContainer       = el("requestContainer");
const responseContainer      = el("responseContainer")

// entity/command editor
const commandValue           = el("commandValue");
const entityContainer        = el("entityContainer");

/* if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("./sw.js").then(registration => {
        console.log("SW registered");
    }).catch(error => {
        console.error(`SW failed: ${error}`);
    });
} */


export class App {
    readonly explorer:  Explorer;
    readonly editor:    EntityEditor;

    constructor() {
        this.explorer   = new Explorer(this.config);
        this.editor     = new EntityEditor();
    }

    connectWebsocket () {
        if (connection) {
            connection.close();
            connection = null;
        }
        const loc     = window.location;
        const nr      = ("" + (++websocketCount)).padStart(3, "0");
        const uri     = `ws://${loc.host}/ws-${nr}`;
        // const uri  = `ws://google.com:8080/`; // test connection timeout
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
            const duration = new Date().getTime() - requestStart;
            const data = JSON.parse(e.data);
            // console.log('server:', e.data);
            switch (data.msg) {
            case "resp":
            case "error":
                clt = data.clt;
                cltElement.innerText    = clt ?? " - ";
                const content           = this.formatJson(this.config.formatResponses, e.data);
                this.responseModel.setValue(content)
                responseState.innerHTML = `· ${duration} ms`;
                break;
            case "ev":
                subscriptionCount.innerText = String(++subCount);
                subSeq = data.seq;
                // multiple clients can use the same WebSocket. Use the latest
                if (clt == data.clt) {
                    subscriptionSeq.innerText   = subSeq ? String(subSeq) : " - ";
                    ackElement.innerText        = subSeq ? String(subSeq) : " - ";
                }
                break;
            }
        };
    }

    closeWebsocket  () {
        connection.close();
    }

    getCookie  (name: string) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2)
            return parts.pop().split(';').shift();
        return null;
    }

    initUserToken  () {
        const user    = this.getCookie("fliox-user")   ?? "admin";
        const token   = this.getCookie("fliox-token")  ?? "admin";
        this.setUser(user);
        this.setToken(token);
    }

    setUser (user: string) {
        defaultUser.value   = user;
        document.cookie = `fliox-user=${user};`;
    }

    setToken  (token: string) {
        defaultToken.value  = token;
        document.cookie = `fliox-token=${token};`;
    }

    selectUser (element: HTMLElement) {
        let value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    };

    addUserToken (jsonRequest: string) {
        const endBracket  = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before      = jsonRequest.substring(0, endBracket);
        const after       = jsonRequest.substring(endBracket);
        let   userToken   = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken       = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    sendSyncRequest () {
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            this.responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
            responseState.innerHTML = "";
        } else {
            let jsonRequest = this.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                const request     = JSON.parse(jsonRequest);
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
        reqIdElement.innerText  =  String(req);
    }

    async postSyncRequest () {
        let jsonRequest         = this.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        let start = new Date().getTime();
        let duration: number;
        try {
            const response  = await this.postRequest(jsonRequest, "POST");
            let content     = await response.text;
            content         = this.formatJson(this.config.formatResponses, content);
            duration        = new Date().getTime() - start;
            this.responseModel.setValue(content);
        } catch(error) {
            duration = new Date().getTime() - start;
            this.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }

    lastCtrlKey:        boolean;
    refLinkDecoration:  CSSStyleRule;

    applyCtrlKey(event: KeyboardEvent) {
        if (this.lastCtrlKey == event.ctrlKey)
            return;
        this.lastCtrlKey = event.ctrlKey;
        if (!this.refLinkDecoration) {
            const cssRules = document.styleSheets[0].cssRules;
            for (let n = 0; n < cssRules.length; n++) {
                const rule = cssRules[n] as CSSStyleRule;
                if (rule.selectorText == ".refLinkDecoration:hover")
                    this.refLinkDecoration = rule;
            }
        }
        this.refLinkDecoration.style.cursor = this.lastCtrlKey ? "pointer" : "";
    }

    onKeyUp (event: KeyboardEvent) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }

    onKeyDown (event: KeyboardEvent) {
        const editor = this.editor;

        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);

        switch (this.config.activeTab) {
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
                        this.execute(event, () => editor.saveEntitiesAction());
                    break;
                case 'KeyP':
                    if (event.ctrlKey && event.altKey)
                        this.execute(event, () => editor.sendCommand("POST"));
                    break;
                case 'ArrowLeft':
                    if (event.altKey)
                        this.execute(event, () => editor.navigateEntity(editor.entityHistoryPos - 1));
                    break;        
                case 'ArrowRight':
                    if (event.altKey)
                        this.execute(event, () => editor.navigateEntity(editor.entityHistoryPos + 1));
                    break;
                case 'Digit1':
                    if (!event.altKey)
                        break;
                    this.switchTab();
                    break;
                }
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.ctrlKey}, alt:${event.altKey}`);
    }

    switchTab () {
        if (document.activeElement == entityExplorer)
            this.entityEditor.focus();
        else
            entityExplorer.focus();
    }

    execute(event: KeyboardEvent, lambda: () => void) {
        lambda();
        event.preventDefault();
    }

    // --------------------------------------- example requests ---------------------------------------
    async onExampleChange () {
        const exampleName = selectExample.value;
        if (exampleName == "") {
            this.requestModel.setValue("")
            return;
        }
        const response = await fetch(exampleName);
        const example = await response.text();
        this.requestModel.setValue(example)
    }

    async loadExampleRequestList () {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option      = createEl("option");
        option.value    = "";
        option.disabled = true;
        option.selected = true;
        option.hidden   = true;
        option.text     = "Select request ...";
        selectExample.add(option);

        const folder    = './example-requests'
        const response  = await fetch(folder);
        if (!response.ok)
            return;
        const exampleRequests   = await response.json();
        let   groupPrefix       = "0";
        let   groupCount        = 0;
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.substring(folder.length).replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = createEl("option");
            option.value                    = example;
            option.text                     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor    = groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }
    // --------------------------------------- Explorer ---------------------------------------
  

    async postRequest (request: string, tag: string) {
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

    async postRequestTasks (database: string, tasks: SyncRequestTask_Union[], tag: string) {
        const db = database == "main_db" ? undefined : database;
        const sync: SyncRequest = {
            "msg":      "sync",
            "database": db,
            "tasks":    tasks,
            "user":     defaultUser.value,
            "token":    defaultToken.value
        }
        const request = JSON.stringify(sync);
        tag = tag ? tag : "";
        return await this.postRequest(request, `${database}/${tag}`);
    }

    static getRestPath(database: string, container: string, ids: string | string[], query: string) {
        let path = `./rest/${database}`;
        if (container)  path = `${path}/${container}`;
        if (ids) {
            if (Array.isArray(ids)) {
                path = `${path}?ids=${ids.join(',')}`;
            } else {
                path = `${path}/${ids}`;
            }
        }
        if (query)      path = `${path}?${query}`;
        return path;
    }

    static async restRequest (method: Method, body: string, database: string, container: string, ids: string | string[], query: string) {
        const path = App.getRestPath(database, container, ids, query);        
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
                status:     0,
                statusText: "exception",
                text:   () : string => error.message,
                json:   ()          => { throw error.message }
            }
        }
    }

    static getTaskError (content: ProtocolResponse_Union, taskIndex: number) {
        if (content.msg == "error") {
            return content.message;
        }
        const task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }

    static bracketValue = /\[(.*?)\]/;

    static errorAsHtml (message: string, p: Resource | null) {
        // first line: error type, second line: error message
        const pos = message.indexOf(' > ');
        let error = message;
        if (pos > 0) {
            let reason = message.substring(pos + 3);
            if (reason.startsWith("at ")) {
                const id = reason.match(App.bracketValue)[1];
                if (p && id) {
                    const c: Resource   = { database: p.database, container: p.container, ids: [id] };
                    const coordinate    = JSON.stringify(c);
                    const link = `<a  href="#" onclick='app.loadEntities(${coordinate})'>${id}</a>`;
                    reason = reason.replace(id, link);
                }
                reason = reason.replace("] ", "]<br>");
            }
            error =  message.substring(0, pos) + " ><br>" + reason;
        }
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }

    static setClass(element: Element, enable: boolean, className: string) {
        const classList = element.classList;
        if (enable) {
            classList.add(className);
            return;
        }
        classList.remove(className);        
    }

    toggleDescription() {
        this.changeConfig("showDescription", !this.config.showDescription);   
        this.openTab(this.config.activeTab);
    }

    openTab (tabName: string) {
        const config            = this.config;
        config.activeTab        = tabName;
        App.setClass(document.body, !config.showDescription, "miniHeader")
        const tabContents       = document.getElementsByClassName("tabContent");
        const tabs              = document.getElementsByClassName("tab");
        const gridTemplateRows  = document.body.style.gridTemplateRows.split(" ");
        const headerHeight      = getComputedStyle(document.body).getPropertyValue('--header-height');
        gridTemplateRows[0]     = config.showDescription ? headerHeight : "0";
        for (let i = 0; i < tabContents.length; i++) {
            const tabContent            = tabContents[i] as HTMLElement;
            const isActiveContent       = tabContent.id == tabName;
            tabContent.style.display    = isActiveContent ? "grid" : "none";
            gridTemplateRows[i + 2]     = isActiveContent ? "1fr" : "0"; // + 2  ->  "body-header" & "body-tabs"
            const isActiveTab           = tabs[i].getAttribute('value') == tabName;
            App.setClass(tabs[i], isActiveTab, "selected");
        }
        document.body.style.gridTemplateRows = gridTemplateRows.join(" ");
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }

    selectedCatalog: HTMLElement;


    hubInfo = { } as DbHubInfo;

    async loadCluster () {
        const tasks: SyncRequestTask_Union[] = [
            { "task": "query",  "container": "containers"},
            { "task": "query",  "container": "schemas"},
            { "task": "query",  "container": "commands"},
            { "task": "command","name": "DbHubInfo" }
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response  = await this.postRequestTasks("cluster", tasks, null);
        const content   = response.json as SyncResponse;
        const error     = App.getTaskError (content, 0);
        if (error) {
            catalogExplorer.innerHTML = App.errorAsHtml(error, null);
            return 
        }
        const dbContainers  = content.containers[0].entities    as DbContainers[];
        const dbSchemas     = content.containers[1].entities    as DbSchema[];
        const dbCommands    = content.containers[2].entities    as DbCommands[];
        const hubInfoResult = content.tasks[3]                  as SendCommandResult;
        this.hubInfo        = hubInfoResult.result              as DbHubInfo;
        //
        let   description   = this.hubInfo.description
        const website       = this.hubInfo.website
        if (description || website) {
            if (!description)
                description = "Website";
            hubInfoEl.innerHTML = website ? `<a href="${website}" target="_blank" rel="noopener noreferrer">${description}</a>` : description;
        }

        const ulCatalogs = createEl('ul');
        ulCatalogs.onclick = (ev) => {
            const path = ev.composedPath() as HTMLElement[];
            const selectedElement = path[0];
            if (selectedElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            path[1].classList.add("active");
            if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
            this.selectedCatalog =selectedElement;
            selectedElement.classList.add("selected");
            const databaseName      = selectedElement.childNodes[1].textContent;
            const commands          = dbCommands.find   (c => c.id == databaseName);
            const containers        = dbContainers.find (c => c.id == databaseName);
            this.editor.listCommands(databaseName, commands, containers);
        }
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liCatalog       = createEl('li');
            if (firstDatabase) {
                firstDatabase = false;
                liCatalog.classList.add("active");
            }
            const liDatabase            = createEl('div');
            const catalogCaret          = createEl('div');
            catalogCaret.classList.value= "caret";
            const catalogLabel          = createEl('span');
            catalogLabel.innerText      = dbContainer.id;
            liDatabase.title            = "database";
            catalogLabel.style.pointerEvents = "none"
            liDatabase.append(catalogCaret)
            liDatabase.append(catalogLabel)
            liCatalog.appendChild(liDatabase);
            ulCatalogs.append(liCatalog);

            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath() as HTMLElement[];
                const selectedElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (selectedElement.tagName.toLowerCase() != "div")
                    return;
                if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
                this.selectedCatalog    = selectedElement;
                this.selectedCatalog.classList.add("selected");
                const containerName     = this.selectedCatalog.innerText.trim();
                const databaseName      = path[3].childNodes[0].childNodes[1].textContent;
                const params: Resource  = { database: databaseName, container: containerName, ids: [] };
                this.editor.clearEntity(databaseName, containerName);
                this.explorer.loadContainer(params, null);
            }
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer       = createEl('li');
                liContainer.title       = "container";
                const containerLabel    = createEl('div');
                containerLabel.innerHTML= "&nbsp;" + containerName;
                liContainer.append(containerLabel)
                ulContainers.append(liContainer);
            }
        }
        const schemaMap     = Schema.createEntitySchemas(this.databaseSchemas, dbSchemas);
        const monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);

        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);

        this.editor.listCommands(dbCommands[0].id, dbCommands[0], dbContainers[0]);
    }

    databaseSchemas: { [key: string]: DbSchema} = {};
    
    getSchemaType(database: string) {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${schema.schemaName}</a>`;
    }

    getSchemaExports(database: string) {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/index.html" target="${database}">Typescript, C#, Kotlin, JSON Schema, HTML</a>`;
    }

    static getType(database: string, def: JsonType) {
        const ns          = def._namespace;
        const name        = def._typeName;
        return `<a title="open type definition in new tab" href="./schema/${database}/html/schema.html#${ns}.${name}" target="${database}">${name}</a>`;
    }

    getEntityType(database: string, container: string) {
        const def  = this.getContainerSchema(database, container);
        if (!def)
            return this.schemaLess;
        return App.getType(database, def);
    }

    getTypeLabel(database: string, type: FieldType) {
        if (type.type) {
            return type.type;
        }
        const def = type._resolvedDef;
        if (def) {
            return App.getType(database, def);
        }
        let result = JSON.stringify(type);
        return result = result == "{}" ? "any" : result;
    }

    schemaLess = '<span title="missing type definition - schema-less database" style="opacity:0.5">unknown</span>';

    static getDatabaseLink(database: string) {
        return `<a title="open database in new tab" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`
    }

    getContainerSchema (database: string, container: string) : JsonType | null{
        const schema = app.databaseSchemas[database];
        if (schema) {
            return schema._containerSchemas[container];
        }
        return null;
    }

    // =======================================================================================================
    filter = {} as {
        database:   string,
        container:  string
    }

    filterOnKeyDown(event: KeyboardEvent) {
        if (event.code != 'Enter')
            return;
        this.applyFilter();
    }

    applyFilter() {
        const database  = this.filter.database;
        const container = this.filter.container;
        const filter    = entityFilter.value;
        const query     = filter.trim() == "" ? null : `filter=${encodeURIComponent(filter)}`;
        const params: Resource    = { database: database, container: container, ids: [] };
        this.saveFilter(database, container, filter)
        this.explorer.loadContainer(params, query);
    }

    removeFilter() {
        const params: Resource  = { database: this.filter.database, container: this.filter.container, ids: [] };
        this.explorer.loadContainer(params, null);
    }

    saveFilter(database: string, container: string, filter: string) {
        const filters = this.config.filters;
        if (filter.trim() == "") {
            const filterDatabase = filters[database];
            if (filterDatabase) {
                delete filterDatabase[container];
            }
        } else {
            if (!filters[database]) filters[database] = {}            
            filters[database][container] = [filter];
        }
        this.setConfig("filters", filters);
    }

    updateFilterLink() {
        const filter    = entityFilter.value;
        const query     = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url       = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        el<HTMLAnchorElement>("filterLink").href = url;
    }


    static parseAst(value: string) : jsonToAst.ValueNode {
        try {
            JSON.parse(value);  // early out on invalid JSON
            // 1.) [json-to-ast - npm] https://www.npmjs.com/package/json-to-ast
            // 2.) bundle.js created fom npm module 'json-to-ast' via:
            //     [node.js - How to use npm modules in browser? is possible to use them even in local (PC) ? - javascript - Stack Overflow] https://stackoverflow.com/questions/49562978/how-to-use-npm-modules-in-browser-is-possible-to-use-them-even-in-local-pc
            // 3.) browserify main.js | uglifyjs > bundle.js
            //     [javascript - How to get minified output with browserify? - Stack Overflow] https://stackoverflow.com/questions/15590702/how-to-get-minified-output-with-browserify
            const ast = parse(value, { loc: true });
            // console.log ("AST", ast);
            return ast;
        } catch (error) {
            console.error("parseAst", error);
        }
        return null;
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
        const schemas: MonacoSchema[] = [];
        try {
            const jsonSchemaResponse  = await fetch("schema/protocol/json-schema.json");
            const jsonSchema          = await jsonSchemaResponse.json();

            for (const schemaName in jsonSchema) {
                const schema          = jsonSchema[schemaName];
                const url             = "protocol/json-schema/" + schemaName;
                const schemaEntry: MonacoSchema = {
                    uri:    "http://" + url,
                    schema: schema            
                }
                schemas.push(schemaEntry);
            }
        } catch (e) {
            console.error ("load json-schema.json failed");
        }
        return schemas;
    }

    requestModel:       monaco.editor.ITextModel;
    responseModel:      monaco.editor.ITextModel;

    requestEditor:      monaco.editor.IStandaloneCodeEditor;
    responseEditor:     monaco.editor.IStandaloneCodeEditor;
    entityEditor:       monaco.editor.IStandaloneCodeEditor;
    commandValueEditor: monaco.editor.IStandaloneCodeEditor;

    allMonacoSchemas: MonacoSchema[] = [];

    addSchemas (monacoSchemas: MonacoSchema[]) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }

    async setupEditors ()
    {
        // this.setExplorerEditor("none");
        
        // --- setup JSON Schema for monaco
        const requestUri      = monaco.Uri.parse("request://jsonRequest.json");   // a made up unique URI for our model
        const responseUri     = monaco.Uri.parse("request://jsonResponse.json");  // a made up unique URI for our model
        const monacoSchemas   = await this.createProtocolSchemas();

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
            this.requestEditor  = monaco.editor.create(requestContainer, { /* model: model */ });
            this.requestModel   = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel (this.requestModel);

            const defaultRequest = `{
    "msg": "sync",
    "tasks": [
        {
        "task":  "command",
        "name":  "DbEcho",
        "value": "Hello World"
        }
    ]
}`;
            this.requestModel.setValue(defaultRequest);
        }

        // --- create response editor
        {
            this.responseEditor = monaco.editor.create(responseContainer, { /* model: model */ });
            this.responseModel  = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel (this.responseModel);
        }

        // --- create entity editor
        {
            this.entityEditor   = monaco.editor.create(entityContainer, { });
            this.entityEditor.onMouseDown((e) => {
                if (!e.event.ctrlKey)
                    return;
                if (this.editor.activeExplorerEditor != "entity")
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
            this.commandValueEditor     = monaco.editor.create(commandValue, { });
            // this.commandValueModel   = monaco.editor.createModel(null, "json");
            // this.commandValueEditor.setModel(this.commandValueModel);
            //this.commandValueEditor.setValue("{}");
        }
        this.editor.initEditor(this.entityEditor, this.commandValueEditor);

        // this.commandResponseModel = monaco.editor.createModel(null, "json");
        this.setEditorOptions();
        window.onresize = () => {
            this.layoutEditors();        
        };
    }

    setEditorOptions() {
        const editorSettings: monaco.editor.IEditorOptions & monaco.editor.IGlobalEditorOptions= {
            lineNumbers:    this.config.showLineNumbers ? "on" : "off",
            minimap:        { enabled: this.config.showMinimap ? true : false },
            theme:          window.appConfig.monacoTheme,
            mouseWheelZoom: true
        }
        this.requestEditor.     updateOptions ({ ...editorSettings });
        this.responseEditor.    updateOptions ({ ...editorSettings });
        this.entityEditor.      updateOptions ({ ...editorSettings });
        this.commandValueEditor.updateOptions ({ ...editorSettings });
    }

    tryFollowLink(value: string, column: number, line: number) {
        try {
            JSON.parse(value);  // early out invalid JSON
            const editor            = this.editor;
            const ast               = parse(value, { loc: true });
            const database          = editor.entityIdentity.database;
            const containerSchema   = this.getContainerSchema(database, editor.entityIdentity.container);

            let entity: Resource;
            EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity || value.type != "Literal")
                    return;
                const start = value.loc.start;
                const end   = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    const literalValue = value.value as string;
                    entity = { database: database, container: container, ids: [literalValue] };
                }
            });
            if (entity) {
                editor.loadEntities(entity, false, null);
            }
        } catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }

    setConfig<K extends ConfigKey>(key: K, value: Config[K]) {
        this.config[key]    = value;
        const elem          = el(key);
        if (elem instanceof HTMLInputElement) {
            elem.value   = value as string;
            elem.checked = value as boolean;
        }
        const valueStr = JSON.stringify(value, null, 2);
        window.localStorage.setItem(key, valueStr);
    }

    getConfig(key: keyof Config) {
        const valueStr = window.localStorage.getItem(key);
        try {
            return JSON.parse(valueStr);
        } catch(e) { }
        return undefined;
    }

    initConfigValue(key: ConfigKey) {
        const value = this.getConfig(key);
        if (value == undefined) {
            this.setConfig(key, this.config[key]);
            return;
        }
        this.setConfig(key, value);
    }

    config = defaultConfig;

    loadConfig() {
        this.initConfigValue("showLineNumbers");
        this.initConfigValue("showMinimap");
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
        this.initConfigValue("showDescription");
        this.initConfigValue("filters");
    }

    changeConfig (key: ConfigKey, value: boolean) {
        this.setConfig(key, value);
        switch (key) {
            case "showLineNumbers":
            case "showMinimap":
                this.setEditorOptions();
                break;
        }
    }

    formatJson(format: boolean, text: string) : string {
        if (format) {
            try {
                // const action = editor.getAction("editor.action.formatDocument");
                // action.run();
                const obj       = JSON.parse(text);
                const formatted = JSON.stringify(obj, null, 4);
                if (!Array.isArray(obj))
                    return formatted;
                let lines   = formatted.split('\n');
                lines       = lines.slice(1, lines.length - 1);
                lines       = lines.map(l => l.substring(4)); // remove 4 leading spaces
                return `[${lines.join('\n')}]`;
            }
            catch (error) {}            
        }
        return text;
    }

    layoutEditors () {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (this.config.activeTab) {
        case "playground":
            const editors = [
                { editor: this.responseEditor,  elem: responseContainer },               
                { editor: this.requestEditor,   elem: requestContainer },
            ]
            this.layoutMonacoEditors(editors);
            break;
        case "explorer":
            // layout from right to left. Otherwise commandValueEditor.clientWidth is 0px;
            const editors2 = [
                { editor: this.entityEditor,        elem: entityContainer },               
                { editor: this.commandValueEditor,  elem: commandValue },
            ]
            this.layoutMonacoEditors(editors2);
            break;
        }
    }

    layoutMonacoEditors(pairs: { editor: monaco.editor.IStandaloneCodeEditor, elem: HTMLElement }[]) {
        for (let n = pairs.length - 1; n >= 0; n--) {
            const pair = pairs[n];
            if (!pair.editor || !pair.elem.children[0]) {
                pairs.splice(n, 1);
            }
        }
        for (const pair of pairs) {
            const child         = pair.elem.children[0] as HTMLElement;
            child.style.width   = "0px";  // required to shrink width.  Found no alternative solution right now.
            child.style.height  = "0px";  // required to shrink height. Found no alternative solution right now.
        }
        for (const pair of pairs) {
            pair.editor.layout();
        }
        // set editor width/height to their container width/height
        for (const pair of pairs) {
            const child         = pair.elem.children[0] as HTMLElement;
            child.style.width   = pair.elem.clientWidth  + "px";
            child.style.height  = pair.elem.clientHeight + "px";
        }
    }

    dragTemplate :  HTMLElement;
    dragBar:        HTMLElement;
    dragOffset:     number;
    dragHorizontal: boolean;

    startDrag(event: MouseEvent, template: string, bar: string, horizontal: boolean) {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset     = horizontal ? event.offsetX : event.offsetY
        this.dragTemplate   = el(template);
        this.dragBar        = el(bar);
        document.body.style.cursor = "ew-resize";
        document.body.onmousemove = (event)  => app.onDrag(event);
        document.body.onmouseup   = ()       => app.endDrag();
        event.preventDefault();
    }

    getGridColumns(xy: number) {
        const prev = this.dragBar.previousElementSibling as HTMLElement;
        xy = xy - (this.dragHorizontal ? prev.offsetLeft : prev.offsetTop);
        if (xy < 20) xy = 20;
        // console.log (`drag x: ${x}`);
        switch (this.dragTemplate.id) {
            case "playground":          return [xy + "px", "var(--bar-width)", "1fr"];
            case "explorer":
                const cols = this.dragTemplate.style.gridTemplateColumns.split(" ");
                switch (this.dragBar.id) { //  [150px var(--bar-width) 200px var(--bar-width) 1fr];
                    case "exBar1":      return [xy + "px", cols[1], cols[2], cols[3]];
                    case "exBar2":      return [cols[0], cols[1], xy + "px", cols[3]];
                }
                break;
            case "explorerEdit":
                this.editor.commandEditWidth = xy + "px";
                return [this.editor.commandEditWidth, "var(--vbar-width)", "1fr"];
        }
        throw `unhandled condition in getGridColumns() id: ${this.dragTemplate?.id}`
    }

    onDrag(event: MouseEvent) {
        if (!this.dragTemplate)
            return;
        // console.log(`  drag: ${event.clientX}`);
        const clientXY  = this.dragHorizontal ? event.clientX : event.clientY;
        const xy        = clientXY - this.dragOffset;
        const cols      = this.getGridColumns(xy);
        if (this.dragHorizontal) {
            this.dragTemplate.style.gridTemplateColumns = cols.join(" ");
        } else {
            this.dragTemplate.style.gridTemplateRows    = cols.join(" ");
        }
        this.layoutEditors();
        event.preventDefault();
    }

    endDrag() {
        if (!this.dragTemplate)
            return;
        document.body.onmousemove   = undefined;
        document.body.onmouseup     = undefined;
        this.dragTemplate           = undefined;
        document.body.style.cursor  = "auto";
    }

    toggleTheme() {
        let mode = document.documentElement.getAttribute('data-theme');
        mode = mode == 'dark' ? 'light' : 'dark'
        window.setTheme(mode)
        this.setEditorOptions();
    }

    initApp() {
        // --- methods without network requests
        this.loadConfig();
        this.initUserToken();
        this.openTab(app.getConfig("activeTab"));

        // --- methods performing network requests - note: methods are not awaited
        this.loadExampleRequestList();
        this.loadCluster();
    }
}

export const app = new App();
window.addEventListener("keydown", event => app.onKeyDown(event), true);
window.addEventListener("keyup",   event => app.onKeyUp(event), true);
