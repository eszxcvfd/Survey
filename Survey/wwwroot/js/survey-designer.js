// Survey Designer JavaScript
let surveyId;
let isDirty = false;

document.addEventListener('DOMContentLoaded', function() {
    surveyId = document.getElementById('surveyId').value;
    initializeSortable();
    
    // Warn before leaving if there are unsaved changes
    window.addEventListener('beforeunload', function(e) {
        if (isDirty) {
            e.preventDefault();
            e.returnValue = '';
        }
    });
});

// Initialize drag and drop for questions
function initializeSortable() {
    const container = document.getElementById('questionsContainer');
    if (!container) return;

    // Simple drag and drop implementation
    let draggedElement = null;

    container.addEventListener('dragstart', function(e) {
        if (e.target.closest('.question-card')) {
            draggedElement = e.target.closest('.question-card');
            draggedElement.style.opacity = '0.5';
        }
    });

    container.addEventListener('dragend', function(e) {
        if (draggedElement) {
            draggedElement.style.opacity = '';
            draggedElement = null;
        }
    });

    container.addEventListener('dragover', function(e) {
        e.preventDefault();
        const afterElement = getDragAfterElement(container, e.clientY);
        const draggable = draggedElement;
        if (afterElement == null) {
            container.appendChild(draggable);
        } else {
            container.insertBefore(draggable, afterElement);
        }
    });

    container.addEventListener('drop', function(e) {
        e.preventDefault();
        reorderQuestions();
    });

    // Make question cards draggable
    const questionCards = container.querySelectorAll('.question-card');
    questionCards.forEach(card => {
        card.setAttribute('draggable', 'true');
    });
}

function getDragAfterElement(container, y) {
    const draggableElements = [...container.querySelectorAll('.question-card:not(.dragging)')];

    return draggableElements.reduce((closest, child) => {
        const box = child.getBoundingClientRect();
        const offset = y - box.top - box.height / 2;
        if (offset < 0 && offset > closest.offset) {
            return { offset: offset, element: child };
        } else {
            return closest;
        }
    }, { offset: Number.NEGATIVE_INFINITY }).element;
}

// Add a new question
async function addQuestion(questionType) {
    try {
        showLoading('Adding question...');

        const response = await fetch('/SurveyDesigner/AddQuestion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                surveyId: surveyId,
                questionType: questionType
            })
        });

        const result = await response.json();
        hideLoading();

        if (result.success) {
            // Reload page to show new question
            location.reload();
        } else {
            showToast(result.message || 'Failed to add question', 'error');
        }
    } catch (error) {
        hideLoading();
        console.error('Error adding question:', error);
        showToast('Failed to add question', 'error');
    }
}

// Update a question
async function updateQuestion(questionId) {
    const card = document.querySelector(`[data-question-id="${questionId}"]`);
    if (!card) return;

    const questionData = extractQuestionData(card);
    
    try {
        const response = await fetch('/SurveyDesigner/UpdateQuestion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(questionData)
        });

        const result = await response.json();

        if (result.success) {
            isDirty = false;
            // Silently updated
        } else {
            showToast(result.message || 'Failed to update question', 'error');
        }
    } catch (error) {
        console.error('Error updating question:', error);
        showToast('Failed to update question', 'error');
    }
}

// Delete a question
async function deleteQuestion(questionId) {
    if (!confirm('Are you sure you want to delete this question?')) {
        return;
    }

    try {
        showLoading('Deleting question...');

        const response = await fetch('/SurveyDesigner/DeleteQuestion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ questionId: questionId })
        });

        const result = await response.json();
        hideLoading();

        if (result.success) {
            location.reload();
        } else {
            showToast(result.message || 'Failed to delete question', 'error');
        }
    } catch (error) {
        hideLoading();
        console.error('Error deleting question:', error);
        showToast('Failed to delete question', 'error');
    }
}

// Duplicate a question
function duplicateQuestion(questionId) {
    showToast('Duplicate feature coming soon', 'info');
}

// Manage logic for a question
function manageLogic(questionId) {
    // Redirect to Branch Logic page with surveyId
    const surveyIdElement = document.getElementById('surveyId');
    if (surveyIdElement) {
        const surveyId = surveyIdElement.value;
        window.location.href = `/BranchLogic/Index?id=${surveyId}`;
    } else {
        console.error('Survey ID not found');
        showToast('Error: Survey ID not found', 'error');
    }
}

// Add an option to a question
function addOption(questionId) {
    const card = document.querySelector(`[data-question-id="${questionId}"]`);
    if (!card) return;

    const optionsList = card.querySelector('.options-list');
    if (!optionsList) return;

    const optionCount = optionsList.querySelectorAll('.option-item').length;
    const newOptionHtml = `
        <div class="option-item" data-option-id="00000000-0000-0000-0000-000000000000" style="display: flex; align-items: center; gap: 8px;">
            <md-icon style="color: var(--md-sys-color-on-surface-variant);">radio_button_unchecked</md-icon>
            <md-outlined-text-field 
                class="option-text-input"
                value="Option ${optionCount + 1}"
                placeholder="Option ${optionCount + 1}"
                style="flex: 1;"
                onchange="updateQuestion('${questionId}')">
            </md-outlined-text-field>
            <md-icon-button onclick="removeOption(this)" title="Remove option">
                <md-icon>close</md-icon>
            </md-icon-button>
        </div>
    `;

    optionsList.insertAdjacentHTML('beforeend', newOptionHtml);
    isDirty = true;
}

// Remove an option
function removeOption(button) {
    const optionItem = button.closest('.option-item');
    const questionCard = button.closest('.question-card');
    
    if (optionItem && questionCard) {
        const questionId = questionCard.getAttribute('data-question-id');
        optionItem.remove();
        updateQuestion(questionId);
    }
}

// Toggle advanced settings
function toggleAdvancedSettings(questionId) {
    const advancedSection = document.getElementById(`advanced-${questionId}`);
    if (advancedSection) {
        advancedSection.style.display = advancedSection.style.display === 'none' ? 'block' : 'none';
    }
}

// Reorder questions after drag and drop
async function reorderQuestions() {
    const container = document.getElementById('questionsContainer');
    const questionCards = container.querySelectorAll('.question-card');
    const questionIds = Array.from(questionCards).map(card => card.getAttribute('data-question-id'));

    try {
        const response = await fetch('/SurveyDesigner/ReorderQuestions', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                surveyId: surveyId,
                questionIds: questionIds
            })
        });

        const result = await response.json();

        if (result.success) {
            renumberQuestions();
        } else {
            showToast(result.message || 'Failed to reorder questions', 'error');
        }
    } catch (error) {
        console.error('Error reordering questions:', error);
        showToast('Failed to reorder questions', 'error');
    }
}

// *** CẬP NHẬT: Preview Survey Function ***
function previewSurvey() {
    const surveyIdElement = document.getElementById('surveyId');
    if (surveyIdElement) {
        const surveyId = surveyIdElement.value;
        window.location.href = `/SurveyDesigner/Preview?id=${surveyId}`;
    } else {
        console.error('Survey ID not found');
        showToast('Error: Survey ID not found', 'error');
    }
}

// Helper functions

function extractQuestionData(card) {
    const questionId = card.getAttribute('data-question-id');
    const questionText = card.querySelector('.question-text-input').value;
    const helpText = card.querySelector('.help-text-input')?.value || '';
    const isRequired = card.querySelector('.required-checkbox')?.checked || false;
    const validationRule = card.querySelector('.validation-rule-input')?.value || '';
    const defaultValue = card.querySelector('.default-value-input')?.value || '';
    const questionType = card.querySelector('.question-type-badge')?.textContent.trim() || '';

    const options = [];
    const optionItems = card.querySelectorAll('.option-item');
    optionItems.forEach((item, index) => {
        const optionId = item.getAttribute('data-option-id');
        const optionText = item.querySelector('.option-text-input').value;
        options.push({
            optionId: optionId === '00000000-0000-0000-0000-000000000000' ? '00000000-0000-0000-0000-000000000000' : optionId,
            questionId: questionId,
            optionText: optionText,
            optionOrder: index + 1,
            isActive: true
        });
    });

    return {
        questionId: questionId,
        surveyId: surveyId,
        questionText: questionText,
        questionType: questionType,
        questionOrder: parseInt(card.getAttribute('data-question-order')),
        isRequired: isRequired,
        helpText: helpText,
        validationRule: validationRule,
        defaultValue: defaultValue,
        options: options
    };
}

function renumberQuestions() {
    const questionCards = document.querySelectorAll('.question-card');
    questionCards.forEach((card, index) => {
        const orderLabel = card.querySelector('.md-typescale-label-large');
        if (orderLabel) {
            orderLabel.textContent = `Question ${index + 1}`;
        }
        card.setAttribute('data-question-order', index + 1);
    });
}

function updateQuestionCount() {
    const count = document.querySelectorAll('.question-card').length;
    const countElement = document.getElementById('questionCount');
    if (countElement) {
        countElement.textContent = count;
    }
}

// UI helper functions
function showLoading(message) {
    console.log('Loading:', message);
}

function hideLoading() {
    console.log('Loading complete');
}

function showToast(message, type) {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <span class="material-symbols-outlined">${type === 'success' ? 'check_circle' : type === 'error' ? 'error' : 'info'}</span>
        <span>${message}</span>
    `;
    toast.style.cssText = `
        position: fixed;
        bottom: 24px;
        right: 24px;
        padding: 16px 24px;
        background-color: ${type === 'success' ? 'var(--md-sys-color-primary-container)' : type === 'error' ? 'var(--md-sys-color-error-container)' : 'var(--md-sys-color-surface-variant)'};
        color: ${type === 'success' ? 'var(--md-sys-color-on-primary-container)' : type === 'error' ? 'var(--md-sys-color-on-error-container)' : 'var(--md-sys-color-on-surface-variant)'};
        border-radius: 8px;
        display: flex;
        align-items: center;
        gap: 12px;
        box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        z-index: 1000;
        transform: translateY(100px);
        opacity: 0;
        transition: all 0.3s ease;
    `;
    document.body.appendChild(toast);
    
    setTimeout(() => {
        toast.style.transform = 'translateY(0)';
        toast.style.opacity = '1';
    }, 10);
    
    setTimeout(() => {
        toast.style.transform = 'translateY(100px)';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}