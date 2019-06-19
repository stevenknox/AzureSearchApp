import "./css/main.css";
import * as signalR from "@aspnet/signalr";
import "bootstrap";
import * as $ from "jquery";

import { SearchResult } from "./models";
import { setTimeout } from "timers";

const tblResults: HTMLTableElement = document.querySelector("#tblResults");
const tbSearch: HTMLInputElement = document.querySelector("#tbSearch");
const btnSearch: HTMLButtonElement = document.querySelector("#btnSearch");
const btnReindex: HTMLButtonElement = document.querySelector("#btnReindex");
const btnAnalyseMedia: HTMLButtonElement = document.querySelector("#btnAnalyseMedia");
const modalHeader: HTMLHeadingElement = document.querySelector("#modalHeader");
const modalDetails: HTMLDivElement = document.querySelector("#modalDetails");
const modalDownloading: HTMLDivElement = document.querySelector("#modalDownloading");
const modalUploading: HTMLDivElement = document.querySelector("#modalUploading");
const notification: HTMLDivElement = document.querySelector("#notification");
const eventsWrapper: HTMLDivElement = document.querySelector("#eventsWrapper");
const events: HTMLPreElement = document.querySelector("#events");
const modalUploadingComplete: HTMLDivElement = document.querySelector("#modalUploadingComplete");
const modalImage: HTMLDivElement = document.querySelector("#modalImage");
const modalVideo: HTMLDivElement = document.querySelector("#modalVideo");
const video: HTMLVideoElement = document.querySelector("#video");
const image: HTMLImageElement = document.querySelector("#image");
const loading: HTMLDivElement = document.querySelector("#loading");
const transcript: HTMLPreElement = document.querySelector("#transcript");
const files: HTMLInputElement = document.querySelector("#files");


const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub")
    .build();

let results: SearchResult[];

$(document).ready(function(){

    var myToast = $("#myToast");
    myToast.toast({
        autohide: false
    });

    $(".hide-toast").click(function(){
        myToast.toast('hide');
    });

    $(".show-toast").click(function(){
        myToast.toast('show');
    });
});

connection.start().catch(err => document.write(err));

connection.on("resultsReceived", (res: SearchResult[]) => {
    results = res;
    console.log(results);
    let m = document.createElement("tbody");

    let result = "";
    
    results.forEach(element => {
        result += `<tr data-index-number=${element.id} class='select-result'><td>${element.id}</td><td>${element.name}</td><td>${element.displayType}</td></tr>`;
    });
    
    m.innerHTML = result;
    tblResults.appendChild(m);
    tblResults.scrollTop = tblResults.scrollHeight;

    loading.hidden = true;
    tblResults.hidden = false;

    $(document).ready(function () {
        $(".select-result").click(function (event) {
            openModal(+event.currentTarget.dataset.indexNumber - 1);
        });
    });
});

connection.on("fileDownloaded", (fileName: string, isImage: boolean) => {
    DisplayOrDownload(fileName, isImage);
});

connection.on("eventReceived", (message: string) => {
    events.innerHTML += `${message}<br />`;
    events.scrollTop = events.scrollHeight;
});

tbSearch.addEventListener("keyup", (e: KeyboardEvent) => {
    if (e.keyCode === 13) {
        search();
    }
});

btnSearch.addEventListener("click", search);

btnReindex.addEventListener("click", reindex);

btnAnalyseMedia.addEventListener("click", analyseMedia);

files.addEventListener("change", uploadFiles);

function search() {
    $("#tblResults tbody").remove();
    loading.hidden = false;
    tblResults.hidden = true;
    connection.send("search", tbSearch.value);
}

function openModal(id: number) {

    var selectedResult = results[id];
    
    console.log(selectedResult);
    
    modalHeader.innerText = selectedResult.name; 

    modalDetails.hidden = true;
    modalVideo.hidden = true;
    modalImage.hidden = true;
    modalDownloading.hidden = true;

    if(+selectedResult.searchType == 0)
    {
        $.ajax({
            url: "api/search/objectinfo",
            type: 'post',
            data: JSON.stringify(selectedResult),
            headers: {
                "Content-Type": 'application/json',
            },
            dataType: 'text',
            success: function (data) {
                console.log("RES", data);
                modalDetails.innerHTML = data;
                modalDetails.hidden = false;
                $("#detailsModal").modal();
            }
        });
    }
    if(+selectedResult.searchType == 1)
    {
        DisplayOrDownload(selectedResult.details, selectedResult.displayType.includes("Image"));
    }
    else if(+selectedResult.searchType == 2)
    {
        var source = document.createElement('source');
        source.setAttribute('src', `Media/${selectedResult.details}`);
        source.setAttribute('type', `video/mp4`);

        var track = document.createElement('track');
        track.setAttribute('kind', `captions`);
        track.setAttribute('srclang', `en`);
        track.setAttribute('label', `English`);
        track.setAttribute('src', `Media/Output/${selectedResult.name}/transcript.vtt`);

        video.appendChild(source);
        video.appendChild(track);

        
        $(document).ready(function () {
            $.get(`api/media/transcript/${selectedResult.name}`, function(data, status){
                transcript.innerHTML = data;
            });
        });
        video.play();

        modalVideo.hidden = false;

        $("#detailsModal").modal();
    }
}

function DisplayOrDownload(fileName: string, isImage :boolean) {
    modalImage.hidden = true;
    modalDownloading.hidden = true;

    let url = `Downloads\\${fileName}`;
    let docExists = urlExists(url);
    if (docExists) {

        if(isImage)
        {
            image.src = url;
            modalImage.hidden = false;

            $("#detailsModal").modal();
        }
        else
        {
            window.open(url, "_blank");
        }
    }
    else {
        modalDownloading.hidden = false;
        connection.send("downloadFile", fileName, isImage);
    }
}

function urlExists(url: string)
{
    var http = new XMLHttpRequest();
    http.open('HEAD', url, false);
    http.send();
    return http.status!=404;
}

function analyseMedia(){
    console.log("Analyse Media 2");
  
}

function reindex() {
    console.log("Starting Reindex");
    var myToast = $("#myToast");
    eventsWrapper.hidden = false;
    
    $.post("/api/search/reindex", function(){
        console.log("Finished Indexing");
        notification.innerText = "Reindexing has completed. Newly indexed data should now appear in search results.";
        myToast.toast('show');

        setTimeout(() => {
            console.log("Tidy Up Indexing");
            myToast.toast('hide');
            eventsWrapper.hidden = true;
            events.innerText = "";
            notification.innerText = "";
        }, 5000);
    })
}


function uploadFiles() {
    var myToast = $("#myToast");
    modalUploading.hidden = false;

    var fileToUpload = files.files;
    var formData = new FormData();
  
    for (var i = 0; i != fileToUpload.length; i++) {
      formData.append("files", fileToUpload[i]);
    }
  
    $.ajax(
      {
        url: "/api/search/uploadfile",
        data: formData,
        processData: false,
        contentType: false,
        type: "POST",
        success: function (data) {
            modalUploading.hidden = true;
            modalUploadingComplete.hidden = false;
            notification.innerText = "File has been uploaded successfully to Azure. Run a Reindex to see the file in search results.";
            $("#uploadModal").modal("hide");
            myToast.toast('show');
            setTimeout(function(){
                modalUploadingComplete.hidden = true;
                myToast.toast('hide');
                notification.innerText = "";
            }, 5000);
        }
      }
    );
  }