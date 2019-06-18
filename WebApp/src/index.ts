import "./css/main.css";
import * as signalR from "@aspnet/signalr";
import "bootstrap";
import * as $ from "jquery";

import { SearchResult } from "./models";

const tblResults: HTMLTableElement = document.querySelector("#tblResults");
const tbSearch: HTMLInputElement = document.querySelector("#tbSearch");
const btnSearch: HTMLButtonElement = document.querySelector("#btnSearch");
const modalHeader: HTMLHeadingElement = document.querySelector("#modalHeader");
const modalDetails: HTMLDivElement = document.querySelector("#modalDetails");
const modalDownloading: HTMLDivElement = document.querySelector("#modalDownloading");
const modalImage: HTMLDivElement = document.querySelector("#modalImage");
const modalVideo: HTMLDivElement = document.querySelector("#modalVideo");
const video: HTMLVideoElement = document.querySelector("#video");
const image: HTMLImageElement = document.querySelector("#image");
const loading: HTMLDivElement = document.querySelector("#loading");
const transcript: HTMLPreElement = document.querySelector("#transcript");

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub")
    .build();

let results: SearchResult[];

connection.start().catch(err => document.write(err));

connection.on("resultsReceived", (res: SearchResult[]) => {
    results = res;
    console.log(results);
    let m = document.createElement("tbody");

    let result = "";
    
    results.forEach(element => {
        result += `<tr data-index-number=${element.id} class='select-result'><td>${element.id}</td><td>${element.name}</td><td>${element.type}</td></tr>`;
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

tbSearch.addEventListener("keyup", (e: KeyboardEvent) => {
    if (e.keyCode === 13) {
        search();
    }
});

btnSearch.addEventListener("click", search);

function search() {
    loading.hidden = false;
    tblResults.hidden = true;
    connection.send("search", tbSearch.value)
    .then(() => tbSearch.value = "");
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
        modalDetails.innerText = selectedResult.details;
        modalDetails.hidden = false;
        
        $("#detailsModal").modal();
    }
    if(+selectedResult.searchType == 1)
    {
        DisplayOrDownload(selectedResult.details, selectedResult.type.includes("Image"));
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