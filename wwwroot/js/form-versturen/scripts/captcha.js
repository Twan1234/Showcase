const form = document.querySelector('#contact-form');

form.addEventListener('submit', onSubmit);

async function onSubmit(e) {
    e.preventDefault();

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
