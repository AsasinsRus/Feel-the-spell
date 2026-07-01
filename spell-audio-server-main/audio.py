#!/usr/bin/env python3

# prerequisites: as described in https://alphacephei.com/vosk/install and also python module `sounddevice` (simply run command `pip install sounddevice`)

import queue
import sys
import sounddevice as sd
import json

from vosk import Model, KaldiRecognizer

def all_audio_devices():
    return sd.query_devices()

class AudioAnalyzer:

    def __init__(self, device: int | None, lang: str | None, single_word: bool = False):
        if device is None:
            self._device = int(sd.query_devices("default")["index"])
        else:
            self._device = device

        device_info = sd.query_devices(self._device, "input")
        self._samplerate = int(device_info["default_samplerate"])
            
        if lang is None:
            self._model = Model(lang="en-us")
        else:
            self._model = Model(lang=lang)

        self._single_word = single_word
        self._last_words = []

        self._q = queue.Queue()

    def _feed_queue(self, indata, frames, time, status):
        """This is called (from a separate thread) for each audio block."""
        if status:
            print(status, file=sys.stderr)
        self._q.put(bytes(indata))

    def listen(self):
        with sd.RawInputStream(samplerate=self._samplerate, blocksize = 8000, device=self._device,
                dtype="int16", channels=1, callback=self._feed_queue):

            rec = KaldiRecognizer(self._model, self._samplerate)

            while True:
                data = self._q.get()
                
                wf = rec.AcceptWaveform(data)

                if wf:
                    self._last_words = []
                    result = rec.Result()

                    if not self._single_word:
                        j = json.loads(result)
                        text: str = j.get("text", "")

                        if text == "":
                            continue

                        yield text
                elif self._single_word:
                    result = rec.PartialResult()
                    j = json.loads(result)
                    text: str = j.get("partial", "")    

                    initial_words = text.split(" ")
                    words = [w for w in initial_words]

                    for word in self._last_words:
                        if len(words) == 0:
                            break

                        if word == words[0]:
                            words.pop(0)

                    for word in words:
                        if word == "":
                            continue
                        
                        yield word

                    self._last_words = initial_words
