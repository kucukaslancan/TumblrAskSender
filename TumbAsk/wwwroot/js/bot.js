let successCount = 0;
let failureCount = 0;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/botStatusHub")
    .withAutomaticReconnect()     .configureLogging(signalR.LogLevel.Information)
    .build();

connection.serverTimeoutInMilliseconds = 300000; connection.keepAliveIntervalInMilliseconds = 15000; 
connection.on("ReceiveStatusUpdate", (botId, status) => {
    document.getElementById("status_" + botId).innerText = status;
});

connection.on("ReceiveLogUpdate", (botId, message, statusResponse) => {
    const logsList = document.getElementById("logsList");
    const logItem = document.createElement("li");
    logItem.className = "list-group-item";
    logItem.style.color = "blue";
    if (statusResponse == 1) {
        logItem.style.color = "green";
        successCount++;
        document.getElementById("successCount").innerText = `Success: ${successCount}`;
    }
    else if (statusResponse == 2){
        failureCount++;
        logItem.style.color = "red";
        document.getElementById("failureCount").innerText = `Fail: ${failureCount}`;
    }


  
    logItem.textContent = `Bot ${botId}: ${message}`;
    
    logsList.insertBefore(logItem, logsList.firstChild);
});

connection.start()
    .then(() => console.log("SignalR connected"))
    .catch(err => console.error("SignalR connection error: ", err));





    document.addEventListener("DOMContentLoaded", function () {
       
        document.querySelectorAll(".start-stop-btn").forEach(button => {
            
            button.addEventListener("click", function () {
                 
                const botId = this.getAttribute("data-bot-id");
                const isStart = this.textContent.includes("Start");

                if (isStart) {
                                        $('#logsModal').modal('show');
                    document.getElementById("logsList").innerHTML = "";
                    document.querySelector(`.live-report-btn[data-bot-id="${botId}"]`).style.display = "inline";
                    this.textContent = "Stop Question";
                    this.classList.remove("btn-primary");
                    this.classList.add("btn-danger");

                    fetch(`/Bot/StartQuestion?botId=${botId}`, { method: "POST" })
                        .then(response => response.json())
                        .then(data => {
                            if (data.success) {
                                
                                
                            } else {
                                alert("Failed to start question sending.");
                            }
                        })
                        .catch(error => {
                            console.error("Start Question error:", error);
                            alert("An error occurred while starting question sending.");
                        });
                } else {
                                        fetch(`/Bot/StopQuestion?botId=${botId}`, { method: "POST" })
                        .then(response => response.json())
                        .then(data => {
                            if (data.success) {
                                this.textContent = "Start Question";
                                this.classList.remove("btn-danger");
                                this.classList.add("btn-primary");
                                document.querySelector(`.live-report-btn[data-bot-id="${botId}"]`).style.display = "none";
                            } else {
                                alert("Failed to stop question sending.");
                            }
                        })
                        .catch(error => {
                            console.error("Stop Question error:", error);
                            alert("An error occurred while stopping question sending.");
                        });
                }
            });
        });
    });

                document.querySelectorAll(".live-report-btn").forEach(button => {
            button.addEventListener("click", function () {
                $('#logsModal').modal('show');
            });
        });

        document.querySelectorAll(".show-logs-btn").forEach(button => {
        button.addEventListener("click", function () {
            const botId = this.getAttribute("data-bot-id");
            window.location.href = `/Bot/ShowQuestionLogs?botId=${botId}`;
        });
    });

