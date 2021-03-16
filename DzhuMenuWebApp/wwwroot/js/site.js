window.onload = () => init();

function init() {
    const increasers = document.getElementsByClassName("increase_count");
    for (const button of increasers) {
        const countElement = button.parentElement.getElementsByClassName("count").item(0);
        button.onclick = () => addEntry(countElement);
    }
    
    const decreasers = document.getElementsByClassName("decrease_count");
    for (const button of decreasers) {
        const countElement = button.parentElement.getElementsByClassName("count").item(0);
        button.onclick = () => removeEntry(countElement);
    }
    
    const timeInput = document.getElementById("time");
    timeInput.value = getInitialTime();
    timeInput.onchange = setTime;

    const usernameInput = document.getElementById("username");
    usernameInput.value = window.localStorage.getItem("username");
    usernameInput.oninput = (e) => setUsername(e.target);
    
    document.getElementById('export').onclick = exportToOutlook;
    
    const toDepositButton = document.getElementById("to_deposit");
    toDepositButton.onclick = addToDeposit;
    
    const depositValueContainer = document.getElementById("deposit");
    depositValueContainer.onclick = setDeposit;

    localStorage.setItem("deposit", localStorage.getItem("deposit") ?? 0);

    calculate();
    fillDeposit();
}

function addEntry(countElement) {
    countElement.value++;
    calculate();
}

function removeEntry(countElement) {
    countElement.value--;
    calculate();
}

function calculate() {
    const entries = Array.from(document.getElementsByName("entry"))
        .map(getDataFromRow)
        .filter(entry => entry.count > 0);
    
    const username = document.getElementById("username").value;
    const time = document.getElementById("time").value.split(":").slice(0, 2).join(":");
    const order = entries.map(entry => `- ${entry.count} ${entry.name}`).join("\n");
    
    const format = `Добрый день,\n\nЗаказ на ${time} с собой:\n${order}\n\nС уважением,\n${username}`;
    document.getElementById("preview").textContent = format;

    const sum = entries.reduce((acc, curr, i, arr) => acc += curr.count * curr.cost, 0);
    document.getElementById("sum").innerText =
        document.getElementById("sum2").innerText = sum.toString();
    
    const remains = 350 - sum;
    document.getElementById("remains").innerText = remains.toString();
    window.remains = remains;
}

function getDataFromRow(tr) {
    return {
        name: tr.getElementsByClassName("name").item(0).innerText,
        cost: tr.getElementsByClassName("cost").item(0).innerText.replace(' р', ''),
        count: tr.getElementsByClassName("count").item(0).value,
    }
}

function getInitialTime() {
    const time1 = new Date();
    time1.setHours(time1.getHours() + 1);
    time1.setMinutes(Math.round(time1.getMinutes() / 10) * 10);
    time1.setSeconds(0);
    
    const time2 = new Date();
    time2.setHours(12, 0, 0);
    
    const maxTime = time1 > time2 ? time1 : time2;
    
    return maxTime.toLocaleTimeString();
}

function setUsername(usernameElement) {
    window.localStorage.setItem("username", usernameElement.value);
    calculate();
}

function setTime() {
    calculate();
}

function exportToOutlook() {
    const email = "ivanova.all@yandex.ru";
    const subject = "Заказ";
    const body = document.getElementById("preview").textContent;
    
    window.location.href = `mailto:${email}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`
}

function addToDeposit() {
    const appendValue = parseInt(document.getElementById("remains").innerText);
    const oldValue = parseInt(localStorage.getItem("deposit"));
    const newValue = oldValue + appendValue;
    
    localStorage.setItem("deposit", newValue);
    fillDeposit();
}

function setDeposit() {
    const oldValue = parseInt(localStorage.getItem("deposit"));
    const newValue = prompt("Введите новое значение депозита", oldValue.toString());
    
    const parsedValue = parseInt(newValue);
    if (isNaN(parsedValue)) {
        return;
    }
    
    localStorage.setItem("deposit", newValue);
    fillDeposit();
}

function fillDeposit() {
    const value = localStorage.getItem("deposit") || 0;
    document.getElementById("deposit").innerText = value;
}