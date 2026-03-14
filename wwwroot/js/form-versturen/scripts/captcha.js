const form = document.querySelector('#contact-form');
if (form) {
    form.addEventListener('submit', onSubmit);
    const resetBtn = form.querySelector('#contact-reset');
    if (resetBtn) resetBtn.addEventListener('click', (e) => { if (!confirm('Weet je zeker dat je het formulier wilt resetten?')) e.preventDefault(); });
}

function hasConsent() {
    const gdprChoice = localStorage.getItem('gdprChoice');
    const gdprStatus = localStorage.getItem('gdpr-status');
    return gdprChoice === 'accept' || gdprStatus === 'accepted';
}

async function onSubmit(e) {
    e.preventDefault();

    if (!hasConsent()) {
        alert('Please accept the Privacy Policy (via the consent banner at the bottom of the page) before sending your message. We do not use your data without your consent.');
        return;
    }

    grecaptcha.ready(async function () {
        try {
            const token = await grecaptcha.execute('6LdfJnspAAAAAMlKRaMHdHHso7zlXlOHfM_vAsNb', { action: 'submit' });
            

            const response = await fetch('http://localhost:3000/captcha', {
                method: "POST",
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ response: token })
            });

            const result = await response.json();
            
            if (result.success) {
                const formData = new FormData(form);
                const formAction = form.getAttribute('action');
                
                await fetch(formAction, {
                    method: 'POST',
                    body: formData
                });
                            

                alert('Bedankt voor uw bericht! We nemen spoedig contact met u op.');
                form.reset();
            } else {
                alert('reCAPTCHA verification failed.');
            }
        } catch (error) {
            console.log('Error occurred:', error);
        }
    });
}
