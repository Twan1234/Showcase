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

app.post('/captcha', async (req, res) => {
    token = req.body.response;

    const secret = "6LdfJnspAAAAAJP_2L-HvTlheZqL4U2Ta0AwYm3S";

    try {
        const response = await fetch(`https://www.google.com/recaptcha/api/siteverify?secret=${secret}&response=${token}`, {
            method: "POST",
            body: JSON.stringify({
                secret: secret,
                response: token
            }),
            headers: {
                'Content-Type': 'application/json'
            }
        });
        // De response vanuit Google (meer info: https://developers.google.com/recaptcha/docs/v3#site_verify_response):
        const result = await response.json();
       // Stuur het resultaat weer terug naar je client
        res.json(result);
    }   catch (e) {
        console.log(e);
    }
});

