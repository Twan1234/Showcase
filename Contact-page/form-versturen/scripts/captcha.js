function Redirect() {
    window.location.assign("http://127.0.0.1:5500/Profiel-page/index.html");
}

// Het formulier met de knop
const form = document.querySelector('#contact-form');

// Koppel er een event listener aan
form.addEventListener('submit', onSubmit);

function onSubmit(e) {
    // Voorkom dat het formulier verstuurd wordt
    e.preventDefault();

    grecaptcha.ready(function () {
        // Vul hier de site sleutel in (de public key)
        grecaptcha.execute('6LdfJnspAAAAAMlKRaMHdHHso7zlXlOHfM_vAsNb', { action: 'submit' }).then(async function (token) {
            try {
                // Verstuur het eerst naar jouw eigen server.
                // Voor dit voorbeeld is een nodejs server bijgevoegd (Zie map server).
                // Je kunt dit voor je showcase ook aanpassen door je eigen server project (bijv. ASP.NET) te gebruiken.
                const response = await fetch('http://localhost:3000/captcha', {
                    method: "POST",
                    body: JSON.stringify({
                        response: token
                    }),
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                });

                // handel het resultaat af en bepaal of je te maken hebt met een mens of een bot.
                // lees hier meer over de response vanuit Google: https://developers.google.com/recaptcha/docs/v3#site_verify_response
                const result = await response.json();
                let humanFactor;
                let isHuman;
                // je bepaalt zelf wat je doet met de score, 0.5 is slechts een voorbeeldwaarde
                if (result.score > 0.5) {
                    humanFactor = 'Het lijkt erop dat je een mens bent, je score is: ' + result.score;
                    isHuman = true;
                }
                else {
                    humanFactor = 'Het lijkt erop dat je geen mens bent, je score is: ' + result.score;
                    isHuman = false;
                }

                if (isHuman) {
                    const formData = new FormData(form);
                    fetch(form.action, {
                        method: 'POST',
                        body: formData
                    })
                        alert('Bedankt voor uw bericht! We nemen spoedig contact met u op.');
                        Redirect();
                }
            }
            catch (e) {
                console.log('Het verzenden van de captcha is mislukt: ' + e.message)
            }
        });
    });
}