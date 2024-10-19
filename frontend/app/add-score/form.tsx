"use client";
import { Input } from "@nextui-org/input";
import { Button } from "@nextui-org/button";
import { Card, CardBody, CardFooter } from "@nextui-org/card";
import { CircularProgress } from "@nextui-org/progress";
import { Spacer } from "@nextui-org/spacer";
import { FormEvent, useState } from "react";
import { ApiBase } from "@/api/address";
import { ScoreModel } from "@/api/types";
import { Score } from "@/components/score";

export default function ScoreForm() {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [value, setValue] = useState<string>("");
  const [error, setError] = useState<string | null>(null)
  const [score, setScore] = useState<ScoreModel | null>(null);

  function setScoreId(val : string){
    setValue(val);
    setError(null);
  }

  async function submit(event : FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsLoading(true);
    setError(null);
    setScore(null);

    var response = await fetch(`${ApiBase}/scores/add?id=${value}`, {
      method: "post",
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json'
      }});

      if (!response.ok) {
        setError(await response.text())
      }
      else{
        setScore(await response.json())
      }

      setIsLoading(false);
  }

  return (
    <Card>
      <CardBody>
        <form className="flex flex-row items-center justify-center" onSubmit={submit}>
          <Input
            label="Score ID"
            placeholder="Enter score ID"
            className="flex-auto"
            isRequired
            value={value}
            size="md"
            onValueChange={setScoreId}
            disabled={isLoading}
            type="number"
          />
          <Spacer x={3} />
          <Button color="primary" className="flex-none" type="submit" disabled={isLoading}>Add</Button>
        </form>
      </CardBody>
      {error ? <CardFooter><span className="w-full text-red-500 text-lg pb-3 text-center">{error.replaceAll("\"", "")}</span></CardFooter> : <></>}
      {isLoading ? <CardFooter className="justify-center"><CircularProgress color="primary" aria-label="Loading..."/></CardFooter> : <></>}
      {score ? <CardFooter><div className="w-full grow"><Score score={score} showPlayer={true}/></div></CardFooter> : <></>}
    </Card>
  );
}
