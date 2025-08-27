//-- SECTION BEGIN: Create Server --
var express = require("express");
const http = require("http");//upgrade port for http use...
const WS_MODULE = require("ws");

const app = express();

// 新增這行來解析 JSON body
app.use(express.json());

app.use(express.static(__dirname + '/public'));
const port = 3000;

app.get("/hello", (req, res) => { res.send("hello world"); });

const server = http.createServer(app);
ws = new WS_MODULE.Server({ server });
server.listen(port, () => { console.log("++ Server turned on, port number:" + port); });

// Initialize WebSocket connection handling
const { clients, rooms, uuidv4, ByteToInt32, ByteToInt16, initializeWebSocketHandling } = require('./core');
initializeWebSocketHandling(ws);
//-- SECTION END: Create Server --

//-- SECTION BEGIN: API to get rooms --
// 新增一個 /rooms API，返回所有房間名稱
app.get("/rooms", (req, res) => {
    const roomInfos = [];
    rooms.forEach((room, roomName) => {
        roomInfos.push({
            roomName: roomName,               // 房間名稱
            roomMasterWSID: room.roomMasterWSID,  // 房主 WebSocket ID
            clients: room.roomClients.size,              // 客戶端的 wsid 陣列
            startTime: room.startTime
        });
    });
    res.json(roomInfos);
});
//-- SECTION END: API to get rooms --

//-- SECTION BEGIN: API to get clients --
app.get("/clients", (req, res) => {
    const clientInfos = [];
    clients.forEach((client, wsid) => {
        clientInfos.push({
            wsid: wsid,
            username: client.username || 'Anonymous'  // 顯示 username
        });
    });
    res.json(clientInfos);
});
//-- SECTION END: API to get clients --

//-- SECTION BEGIN: API to get username by wsid --
app.get("/clients/get-username", (req, res) => {
    const wsid = req.query.wsid;
    const client = clients.get(wsid);

    if (client) {
        res.json({
            wsid: wsid,
            username: client.username || 'Anonymous' // 顯示 username
        });
    } else {
        res.status(404).json({ error: "Client not found" });
    }
});
//-- SECTION END: API to get username by wsid --

//-- SECTION BEGIN: API to set clients --
app.post("/set-username", (req, res) => {
    const { wsid, username } = req.body;  // 確認這裡有解析到 wsid 和 username
    console.log(req.body);  // 用於調試，查看接收到的請求內容

    if (clients.has(wsid)) {
        const client = clients.get(wsid);
        client.username = username; // 更新 username
        res.json({ status: "success", message: "Username updated" });
    } else {
        res.status(404).json({ status: "error", message: "Client not found" });
    }
});
//-- SECTION END: API to set clients --
