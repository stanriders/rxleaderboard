"use client";
import { Textarea } from "@nextui-org/input";
import { Button } from "@nextui-org/button";
import { Card, CardBody, Spacer } from "@nextui-org/react";
import { useState } from "react";

export default function ScoreForm() {
  const [value, setValue] = useState("");

  return (
    <Card>
      <CardBody>
        <div className="flex flex-row items-center justify-center">
          <Textarea
            label="Score link"
            placeholder="Enter score link"
            className="flex-auto"
            isRequired
            value={value}
            size="sm"
            maxRows={1}
            minRows={1}
            onValueChange={setValue}
          />
          <Spacer x={3} />
          <Button color="primary" className="flex-none">Add</Button>
        </div>
      </CardBody>
    </Card>
  );
}
