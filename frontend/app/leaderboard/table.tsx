"use client";
import {
  Table,
  TableHeader,
  TableColumn,
  TableBody,
  TableRow,
  TableCell,
} from "@nextui-org/table";
import { Pagination } from "@nextui-org/pagination";
import { Skeleton } from "@nextui-org/skeleton";
import { Input } from "@nextui-org/input";
import { Spacer } from "@nextui-org/spacer";
import { FC, useEffect, useState } from "react";
import { Select, SelectItem } from "@nextui-org/select";
import useSWR from "swr";
import { useRouter, useSearchParams, usePathname } from "next/navigation";

import { Flag } from "@/components/flag";
import { ApiBase } from "@/api/address";
import { User } from "@/components/user";
import { UserModel } from "@/api/types";

const fetcher = (url: any) =>
  fetch(url)
    .then((r) => r.json())
    .catch((error) => console.log("Leaderboard fetch error: " + error));

type Props = { countries: string[] | undefined };

export const LeaderboardTable: FC<Props> = (props) => {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const [page, setPage] = useState(Number(searchParams.get("page")) ?? 1);
  const [search, setSearch] = useState(searchParams.get("search") ?? "");
  const [country, setCountry] = useState(searchParams.get("country") ?? "");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const countries = ["—"].concat(props.countries ? props.countries : []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedSearch(search);
      setCountry("");
      setPage(1);
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [search, 500]);

  useEffect(() => {
    const params = new URLSearchParams();

    if (search) params.set("search", search);

    if (page) params.set("page", page.toString());

    if (country && country != "—") params.set("country", country);

    router.push(pathname + "?" + params.toString());
  }, [page, search, country]);

  useEffect(() => {
    if (page < 1) setPage(1);
  }, [page]);

  const handleCountrySelectionChange = (e: any) => {
    if (e.target.value == "—") {
      setCountry("");
    } else {
      setCountry(e.target.value);
    }
    setPage(1);
  };

  let address = `${ApiBase}/players?page=${page}`;

  if (debouncedSearch != "") {
    address += `&search=${debouncedSearch}`;
  }
  if (country != "" && country != "—") {
    address += `&countryCode=${country}`;
  }
  const { data, error } = useSWR(address, fetcher);

  const placeholderRows = Array.from(Array(25), (_, i) => (
    <TableRow key={i}>
      <TableCell>
        <Skeleton className="rounded-md">
          <div className="h-6" />
        </Skeleton>
      </TableCell>
      <TableCell>
        <Skeleton className="rounded-md">
          <div className="h-6" />
        </Skeleton>
      </TableCell>
      <TableCell>
        <Skeleton className="rounded-md">
          <div className="h-6" />
        </Skeleton>
      </TableCell>
      <TableCell>
        <Skeleton className="rounded-md">
          <div className="h-6" />
        </Skeleton>
      </TableCell>
    </TableRow>
  ));

  if (error) return <div>Failed to load</div>;
  if (!data)
    return (
      <>
        <div className="w-full flex">
          {props.countries ? (
            <div className="w-full flex justify-start">
              <Select
                disabled
                className="md:ml-4 max-w-52"
                label="Country"
                placeholder="Select a country"
                selectedKeys={[country]}
                selectionMode="single"
                size="sm"
              >
                <SelectItem key="country-placeholder">
                  country-placeholder
                </SelectItem>
              </Select>
            </div>
          ) : (
            <></>
          )}
          <Input
            disabled
            classNames={{
              base: "md:mr-4 max-w-52 justify-end",
              mainWrapper: "h-full",
              input: "text-small",
              inputWrapper:
                "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
            }}
            placeholder="Search player..."
            size="sm"
            type="search"
            value={debouncedSearch}
          />
        </div>
        <Spacer y={2} />
        <Table
          isCompact
          bottomContent={
            <div className="flex w-full justify-center">
              <Pagination
                isCompact
                isDisabled
                showControls
                showShadow
                color="secondary"
                total={0}
              />
            </div>
          }
          fullWidth={true}
        >
          <TableHeader>
            <TableColumn align="start" width={55}>
              {""}
            </TableColumn>
            <TableColumn className="text-ellipsis">Username</TableColumn>
            <TableColumn align="center">Accuracy</TableColumn>
            <TableColumn align="center">PP</TableColumn>
          </TableHeader>
          <TableBody>{placeholderRows}</TableBody>
        </Table>
      </>
    );

  const pages = Math.max(1, Math.ceil(data.total / 50));
  const offset = (page - 1) * 50;
  let players = data.players;

  return (
    <>
      <div className="w-full flex gap-2">
        {props.countries ? (
          <div className="w-full flex justify-start">
            <Select
              className="md:ml-4 max-w-52"
              label="Country"
              placeholder="Select a country"
              selectedKeys={[country]}
              selectionMode="single"
              size="sm"
              onChange={handleCountrySelectionChange}
            >
              {countries.map((country) => (
                <SelectItem key={country} textValue={country}>
                  <div className="flex flex-row">
                    {country != "—" ? (
                      <>
                        <Flag country={country} width={18} />
                        <Spacer x={1} />
                      </>
                    ) : (
                      <></>
                    )}
                    {country}
                  </div>
                </SelectItem>
              ))}
            </Select>
          </div>
        ) : (
          <></>
        )}
        <Input
          classNames={{
            base: "md:mr-4 max-w-52 justify-end",
            mainWrapper: "h-full",
            input: "text-small",
            inputWrapper:
              "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
          }}
          placeholder="Search player..."
          size="sm"
          type="search"
          value={search}
          onValueChange={setSearch}
        />
      </div>
      <Spacer y={2} />
      <Table
        isCompact
        isStriped
        bottomContent={
          <div className="flex w-full justify-center">
            <Pagination
              isCompact
              showControls
              showShadow
              color="primary"
              page={page}
              total={pages}
              onChange={(page) => {
                setPage(page);
              }}
            />
          </div>
        }
        fullWidth={true}
      >
        <TableHeader>
          <TableColumn align="start" width={55}>
            {""}
          </TableColumn>
          <TableColumn>Username</TableColumn>
          <TableColumn align="center">Accuracy</TableColumn>
          <TableColumn align="center">PP</TableColumn>
        </TableHeader>
        <TableBody>
          {players.map((row: UserModel, index: number) => (
            <TableRow key={row.id}>
              <TableCell className="text-default-500">
                #{offset + index + 1}
              </TableCell>
              <TableCell>
                <User user={row} />
              </TableCell>
              <TableCell>
                <p className="text-default-500">
                  {row.totalAccuracy === null
                    ? "-"
                    : row.totalAccuracy?.toFixed(2)}
                  %
                </p>
              </TableCell>
              <TableCell className="text-primary-400 font-semibold">
                {row.totalPp === null ? "-" : row.totalPp?.toFixed(0)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </>
  );
};
