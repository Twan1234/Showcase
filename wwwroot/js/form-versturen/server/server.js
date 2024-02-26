const express = require('express');

const app = express();
const port = 3000;

let cors = require('cors')
app.use(cors());
app.use(express.json());

var bodyParser = require('body-parser');

app.use(bodyParser.urlencoded({
    extended: true
}));

app.use(bodyParser.json());

app.post
app.listen(port, () => console.log(`Server listening on port ${port}!`));

// app.get('/gdpr', (req, res) => {
//     res.setHeader('Set-Cookie', 'gdpr=1; path=/; expires=Fri, 1 Nov 2024 23:59:59 GMT');
//     res.json('OK');
// });

app.post('/captcha', async (req, res) => {
    const token = req.body.response;
    const secret = "6LdfJnspAAAAAJP_2L-HvTlheZqL4U2Ta0AwYm3S";

    try {
        const response = await fetch(`https://www.google.com/recaptcha/api/siteverify?secret=${secret}&response=${token}`, {
            method: "POST",
        });
        const result = await response.json();
        res.json(result);
    } catch (e) {
        console.log(e);
        res.status(500).json({ error: 'An error occurred' });
    }
});

