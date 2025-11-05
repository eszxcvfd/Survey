// Survey Preview JavaScript - Interactive Mode
let currentQuestionIndex = 0;
let questions = [];
let answers = {};

document.addEventListener('DOMContentLoaded', function() {
    // Load questions from data attribute
    const previewContainer = document.getElementById('previewContainer');
    if (previewContainer) {
        questions = JSON.parse(previewContainer.getAttribute('data-questions'));
        console.log('Loaded questions:', questions);
        showQuestion(0);
    }
});

function showQuestion(index) {
    if (index < 0 || index >= questions.length) {
        console.error('Invalid question index:', index);
        return;
    }

    currentQuestionIndex = index;
    const question = questions[index];
    
    // Update question container
    const questionContainer = document.getElementById('currentQuestionContainer');
    questionContainer.innerHTML = renderQuestion(question);
    
    // Update button states
    updateButtonStates();
    
    // Restore previous answer if exists
    restoreAnswer(question.questionId);
}

function renderQuestion(question) {
    let inputHtml = '';
    
    switch (question.questionType) {
        case 'ShortText':
            inputHtml = `
                <md-outlined-text-field 
                    id="answer-${question.questionId}"
                    label="Your answer"
                    style="width: 100%;"
                    ${question.isRequired ? 'required' : ''}>
                </md-outlined-text-field>
            `;
            break;

        case 'LongText':
            inputHtml = `
                <md-outlined-text-field 
                    id="answer-${question.questionId}"
                    label="Your answer"
                    type="textarea"
                    rows="5"
                    style="width: 100%;"
                    ${question.isRequired ? 'required' : ''}>
                </md-outlined-text-field>
            `;
            break;

        case 'Number':
            inputHtml = `
                <md-outlined-text-field 
                    id="answer-${question.questionId}"
                    label="Your answer"
                    type="number"
                    style="width: 100%;"
                    ${question.isRequired ? 'required' : ''}>
                </md-outlined-text-field>
            `;
            break;

        case 'Email':
            inputHtml = `
                <md-outlined-text-field 
                    id="answer-${question.questionId}"
                    label="Email address"
                    type="email"
                    style="width: 100%;"
                    ${question.isRequired ? 'required' : ''}>
                </md-outlined-text-field>
            `;
            break;

        case 'Date':
            inputHtml = `
                <input 
                    type="date" 
                    id="answer-${question.questionId}"
                    class="form-control"
                    style="width: 100%; padding: 16px; border: 1px solid var(--md-sys-color-outline); border-radius: 4px; font-size: 16px;"
                    ${question.isRequired ? 'required' : ''} />
            `;
            break;

        case 'MultipleChoice':
            inputHtml = `
                <div id="answer-${question.questionId}" style="display: flex; flex-direction: column; gap: 12px;">
                    ${question.options.map(option => `
                        <label class="option-label" style="display: flex; align-items: center; gap: 12px; padding: 16px; border: 2px solid var(--md-sys-color-outline-variant); border-radius: 12px; cursor: pointer; transition: all 0.2s;">
                            <input type="radio" name="q-${question.questionId}" value="${option.optionId}" ${question.isRequired ? 'required' : ''} style="width: 20px; height: 20px;" />
                            <span class="md-typescale-body-large">${option.optionText}</span>
                        </label>
                    `).join('')}
                </div>
            `;
            break;

        case 'Checkboxes':
            inputHtml = `
                <div id="answer-${question.questionId}" style="display: flex; flex-direction: column; gap: 12px;">
                    ${question.options.map(option => `
                        <label class="option-label" style="display: flex; align-items: center; gap: 12px; padding: 16px; border: 2px solid var(--md-sys-color-outline-variant); border-radius: 12px; cursor: pointer; transition: all 0.2s;">
                            <input type="checkbox" name="q-${question.questionId}" value="${option.optionId}" style="width: 20px; height: 20px;" />
                            <span class="md-typescale-body-large">${option.optionText}</span>
                        </label>
                    `).join('')}
                </div>
            `;
            break;

        case 'Dropdown':
            inputHtml = `
                <select id="answer-${question.questionId}" 
                        class="form-select" 
                        ${question.isRequired ? 'required' : ''}
                        style="width: 100%; padding: 16px; border: 1px solid var(--md-sys-color-outline); border-radius: 4px; font-size: 16px; background: white;">
                    <option value="">-- Select an option --</option>
                    ${question.options.map(option => `
                        <option value="${option.optionId}">${option.optionText}</option>
                    `).join('')}
                </select>
            `;
            break;

        case 'RatingScale':
            inputHtml = `
                <div id="answer-${question.questionId}" style="display: flex; gap: 8px; justify-content: center; flex-wrap: wrap;">
                    ${question.options.map((option, i) => `
                        <label class="rating-label" style="display: flex; flex-direction: column; align-items: center; cursor: pointer;">
                            <input type="radio" name="q-${question.questionId}" value="${option.optionId}" ${question.isRequired ? 'required' : ''} style="display: none;" />
                            <div class="rating-circle" style="width: 60px; height: 60px; border: 3px solid var(--md-sys-color-primary); border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 24px; font-weight: 600; transition: all 0.2s;">
                                ${i + 1}
                            </div>
                            <span class="md-typescale-label-small" style="margin-top: 4px;">${option.optionText}</span>
                        </label>
                    `).join('')}
                </div>
            `;
            break;

        default:
            inputHtml = '<p style="color: var(--md-sys-color-error);">Unsupported question type: ' + question.questionType + '</p>';
    }

    return `
        <div style="margin-bottom: 24px;">
            <h2 class="md-typescale-title-large" style="margin-bottom: 8px;">
                ${question.questionText}
                ${question.isRequired ? '<span style="color: var(--md-sys-color-error);">*</span>' : ''}
            </h2>
            ${question.helpText ? `<p class="md-typescale-body-small" style="color: var(--md-sys-color-on-surface-variant);">${question.helpText}</p>` : ''}
        </div>
        <div style="margin-bottom: 32px;">
            ${inputHtml}
        </div>
    `;
}

function nextQuestion() {
    // Save current answer
    const currentQuestion = questions[currentQuestionIndex];
    saveAnswer(currentQuestion);
    
    // Validate if required
    if (currentQuestion.isRequired && !isAnswered(currentQuestion)) {
        alert('This question is required. Please provide an answer.');
        return;
    }
    
    // Show next question or complete
    if (currentQuestionIndex < questions.length - 1) {
        showQuestion(currentQuestionIndex + 1);
    } else {
        showCompletionScreen();
    }
}

function previousQuestion() {
    if (currentQuestionIndex > 0) {
        showQuestion(currentQuestionIndex - 1);
    }
}

function saveAnswer(question) {
    const answerId = `answer-${question.questionId}`;
    
    switch (question.questionType) {
        case 'ShortText':
        case 'LongText':
        case 'Number':
        case 'Email':
        case 'Date':
            const textInput = document.getElementById(answerId);
            if (textInput) {
                answers[question.questionId] = { type: 'text', value: textInput.value };
            }
            break;

        case 'MultipleChoice':
        case 'Dropdown':
            const selectedOption = document.querySelector(`input[name="q-${question.questionId}"]:checked`) || 
                                 document.getElementById(answerId);
            if (selectedOption && selectedOption.value) {
                answers[question.questionId] = { type: 'single', value: selectedOption.value };
            }
            break;

        case 'Checkboxes':
            const checkedBoxes = document.querySelectorAll(`input[name="q-${question.questionId}"]:checked`);
            const values = Array.from(checkedBoxes).map(cb => cb.value);
            answers[question.questionId] = { type: 'multiple', values: values };
            break;

        case 'RatingScale':
            const ratingInput = document.querySelector(`input[name="q-${question.questionId}"]:checked`);
            if (ratingInput) {
                answers[question.questionId] = { type: 'rating', value: ratingInput.value };
            }
            break;
    }
    
    console.log('Saved answers:', answers);
}

function restoreAnswer(questionId) {
    const savedAnswer = answers[questionId];
    if (!savedAnswer) return;
    
    const answerId = `answer-${questionId}`;
    
    switch (savedAnswer.type) {
        case 'text':
            const textInput = document.getElementById(answerId);
            if (textInput) {
                textInput.value = savedAnswer.value;
            }
            break;

        case 'single':
            const radioInput = document.querySelector(`input[name="q-${questionId}"][value="${savedAnswer.value}"]`);
            if (radioInput) {
                radioInput.checked = true;
            }
            const selectInput = document.getElementById(answerId);
            if (selectInput && selectInput.tagName === 'SELECT') {
                selectInput.value = savedAnswer.value;
            }
            break;

        case 'multiple':
            savedAnswer.values.forEach(value => {
                const checkbox = document.querySelector(`input[name="q-${questionId}"][value="${value}"]`);
                if (checkbox) {
                    checkbox.checked = true;
                }
            });
            break;

        case 'rating':
            const ratingInput = document.querySelector(`input[name="q-${questionId}"][value="${savedAnswer.value}"]`);
            if (ratingInput) {
                ratingInput.checked = true;
            }
            break;
    }
}

function isAnswered(question) {
    const savedAnswer = answers[question.questionId];
    if (!savedAnswer) return false;
    
    switch (savedAnswer.type) {
        case 'text':
            return savedAnswer.value && savedAnswer.value.trim() !== '';
        case 'single':
        case 'rating':
            return savedAnswer.value !== '';
        case 'multiple':
            return savedAnswer.values && savedAnswer.values.length > 0;
        default:
            return false;
    }
}

function updateButtonStates() {
    const current = currentQuestionIndex + 1;
    const total = questions.length;
    
    // Update button visibility and text
    const prevButton = document.getElementById('prevButton');
    const nextButton = document.getElementById('nextButton');
    
    if (prevButton) {
        prevButton.style.display = current === 1 ? 'none' : 'flex';
    }
    
    if (nextButton) {
        nextButton.innerHTML = current === total ? 
            '<md-icon slot="icon">check</md-icon> Finish Preview' : 
            'Next <md-icon slot="icon">arrow_forward</md-icon>';
    }
}

function showCompletionScreen() {
    const container = document.getElementById('currentQuestionContainer');
    container.innerHTML = `
        <div style="text-align: center; padding: 48px 0;">
            <div style="width: 80px; height: 80px; margin: 0 auto 24px; background: linear-gradient(135deg, #10b981, #059669); border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                <span class="material-symbols-outlined" style="font-size: 48px; color: white;">check_circle</span>
            </div>
            <h2 class="md-typescale-headline-medium" style="margin-bottom: 16px;">Preview Completed!</h2>
            <p class="md-typescale-body-large" style="color: var(--md-sys-color-on-surface-variant); margin-bottom: 32px;">
                You've previewed all questions in this survey.
            </p>
            <p class="md-typescale-body-medium" style="color: var(--md-sys-color-on-surface-variant); margin-bottom: 24px;">
                Total answers collected: <strong>${Object.keys(answers).length} / ${questions.length}</strong>
            </p>
            <div style="display: flex; gap: 12px; justify-content: center;">
                <md-outlined-button onclick="location.reload()">
                    <md-icon slot="icon">refresh</md-icon>
                    Preview Again
                </md-outlined-button>
                <md-filled-button onclick="window.location.href='/SurveyDesigner/Index?id=${questions[0].surveyId}'">
                    <md-icon slot="icon">edit</md-icon>
                    Back to Editor
                </md-filled-button>
            </div>
        </div>
    `;
    
    // Hide navigation buttons
    document.getElementById('navigationButtons').style.display = 'none';
}

// Add CSS styles
const style = document.createElement('style');
style.textContent = `
    .option-label:hover {
        border-color: var(--md-sys-color-primary);
        background-color: var(--md-sys-color-primary-container);
    }

    .option-label input:checked + span {
        font-weight: 600;
        color: var(--md-sys-color-primary);
    }

    .rating-label input:checked ~ .rating-circle {
        background-color: var(--md-sys-color-primary);
        color: var(--md-sys-color-on-primary);
        transform: scale(1.1);
    }

    .rating-circle:hover {
        transform: scale(1.05);
        background-color: var(--md-sys-color-primary-container);
    }
`;
document.head.appendChild(style);