// Const gang
const { Server } = require('kirbe');
const app        = new Server();

// Good CDN for good person like osher
let baseURL = 'https://cdn.derpyenterprises.org'; 

// Route all channels to MafiaWorks Ltd
app.get('/', (req, res) => res.body('Shalom shalom you have reached Derpy Enterprises customer support! If kimi no na wa is not Speyd3r then please leave immediately and go back to looking at Nekopara porn. Arigato!').end());

// DO I LOOK LIKE I KNOW WHAT A JPEG IS
app.get('/takagi', (req, res) => res.body(`${baseURL}/miku/takagi/image(${Math.floor((Math.random() * 48) + 1)}).jpeg`).end());
app.get('/sao', (req, res) => res.body(`${baseURL}/miku/sao/image(${Math.floor((Math.random() * 32) + 1)}).jpg`).end());
app.get('/ddlc', (req, res) => res.body(`${baseURL}/miku/ddlc/image(${Math.floor((Math.random() * 27) + 1)}).jpg`).end());
app.get('/nekopara', (req, res) => res.body(`${baseURL}/miku/nekopara/image(${Math.floor((Math.random() * 224) + 1)}).jpg`).end());

// HOW DO I PRONOUNCE GIF
app.get('/konosuba', (req, res) => res.body(`${baseURL}/miku/konosuba/image(${Math.floor((Math.random() * 51) + 1)}).gif`).end());
app.get('/lovelive', (req, res) => res.body(`${baseURL}/miku/lovelive/image(${Math.floor((Math.random() * 103) + 1)}).gif`).end());
app.get('/k_on', (req, res) => res.body(`${baseURL}/miku/k_on/image(${Math.floor((Math.random() * 142) + 1)}).gif`).end());
app.get('/nekoparagif', (req, res) => res.body(`${baseURL}/miku/nekoparagif/image(${Math.floor((Math.random() * 38) + 1)}).gif`).end());

// JSON EDITION
app.get('/takagijson', (req, res) => res.body({ url: `${baseURL}/miku/takagi/image(${Math.floor((Math.random() * 48) + 1)}).jpeg`}).end());
app.get('/saojson', (req, res) => res.body({ url: `${baseURL}/miku/sao/image(${Math.floor((Math.random() * 32) + 1)}).jpg`}).end());
app.get('/ddlcjson', (req, res) => res.body({ url: `${baseURL}/miku/ddlc/image(${Math.floor((Math.random() * 27) + 1)}).jpg`}).end());
app.get('/nekoparajson', (req, res) => res.body({ url: `${baseURL}/miku/nekopara/image(${Math.floor((Math.random() * 224) + 1)}).jpg`}).end());

app.get('/konosubajson', (req, res) => res.body({ url: `${baseURL}/miku/konosuba/image(${Math.floor((Math.random() * 51) + 1)}).gif`}).end());
app.get('/lovelivejson', (req, res) => res.body({ url: `${baseURL}/miku/lovelive/image(${Math.floor((Math.random() * 103) + 1)}).gif`}).end());
app.get('/k_onjson', (req, res) => res.body({ url: `${baseURL}/miku/k_on/image(${Math.floor((Math.random() * 142) + 1)}).gif`}).end());
app.get('/nekoparagifjson', (req, res) => res.body({ url: `${baseURL}/miku/nekoparagif/image(${Math.floor((Math.random() * 38) + 1)}).gif`}).end());

// Cuddles on port 3939!
app.listen(3939);